using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Events;
using Indice.Features.Cases.Exceptions;
using Indice.Features.Cases.Extensions;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services;

internal class AdminCaseService : BaseCaseService, IAdminCaseService
{
    private const string SchemaKey = "backoffice";
    private readonly CasesDbContext _dbContext;
    private readonly ICaseAuthorizationProvider _memberAuthorizationProvider;
    private readonly ICaseTypeService _caseTypeService;
    private readonly IAdminCaseMessageService _adminCaseMessageService;
    private readonly ICaseEventService _caseEventService;
    private readonly AdminCasesApiOptions _options;
    public AdminCaseService(
        CasesDbContext dbContext,
        AdminCasesApiOptions options,
        ICaseAuthorizationProvider memberAuthorizationProvider,
        ICaseTypeService caseTypeService,
        IAdminCaseMessageService adminCaseMessageService,
        ICaseEventService caseEventService) : base(dbContext, options) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _memberAuthorizationProvider = memberAuthorizationProvider ?? throw new ArgumentNullException(nameof(memberAuthorizationProvider));
        _caseTypeService = caseTypeService ?? throw new ArgumentNullException(nameof(caseTypeService));
        _adminCaseMessageService = adminCaseMessageService ?? throw new ArgumentNullException(nameof(adminCaseMessageService));
        _caseEventService = caseEventService ?? throw new ArgumentNullException(nameof(caseEventService));
        _options = options;
    }

    public async Task<Guid> CreateDraft(ClaimsPrincipal user,
        string caseTypeCode,
        string groupId,
        CustomerMeta customer,
        Dictionary<string, string> metadata) {
        var caseType = await _caseTypeService.Get(caseTypeCode);
        var entity = await CreateDraftInternal(
            _adminCaseMessageService,
            user,
            caseType,
            groupId,
            customer,
            metadata,
            CasesApiConstants.Channels.Agent,
            user);
        return entity.Id;
    }

    public async Task UpdateData(ClaimsPrincipal user, Guid caseId, dynamic data) {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (caseId == default) throw new ArgumentNullException(nameof(caseId));
        if (data == null) throw new ArgumentNullException(nameof(data));
        await _adminCaseMessageService.Send(caseId, user, new Message { Data = data });
    }

    public async Task Submit(ClaimsPrincipal user, Guid caseId) {
        if (caseId == default) throw new ArgumentNullException(nameof(caseId));

        var @case = await _dbContext
            .Cases
            .Include(c => c.CaseType)
            .FirstOrDefaultAsync(c => c.Id == caseId);
        if (@case == null) {
            throw new ArgumentNullException(nameof(@case), @"Case does not exist.");
        }
        if (!@case.Draft) {
            throw new Exception("Case is submitted."); // todo proper exception (BadRequest)
        }

        @case.Draft = false;
        await _dbContext.SaveChangesAsync();
        await _caseEventService.Publish(new CaseSubmittedEvent(@case, @case.CaseType.Code));
    }

    public async Task<ResultSet<CasePartial>> GetCases(ClaimsPrincipal user, ListOptions<GetCasesListFilter> options) {

        // if client is systemic or admin, then bypass checks since no filtering is required.
        var isSystemOrAdmin = ((user.HasClaim(BasicClaimTypes.Scope, CasesApiConstants.Scope) && user.IsSystemClient()) || user.IsAdmin());

        var userId = user.FindSubjectIdOrClientId();
        string inputGroupId = user.FindFirstValue(_options.GroupIdClaimType);
        var userRoles = user.GetUserRoles();

        var queryCases = _dbContext.Cases
            .AsNoTracking()
            .Where(c => !c.Draft) // filter out draft cases
            .Where(options.Filter.Metadata); // filter Metadata

        IQueryable<CasePartial> query;
        if (isSystemOrAdmin) {
            query = queryCases
                .Select(@case => new CasePartial {
                    Id = @case.Id,
                    ReferenceNumber = @case.ReferenceNumber,
                    CustomerId = @case.Customer.CustomerId,
                    CustomerName = @case.Customer.FirstName + " " + @case.Customer.LastName, // concat like this to enable searching with "contains"
                    CreatedByWhen = @case.CreatedBy.When,
                    CaseType = new CaseTypePartial {
                        Id = @case.CaseType.Id,
                        Code = @case.CaseType.Code,
                        Title = @case.CaseType.Title,
                        Translations = TranslationDictionary<CaseTypeTranslation>.FromJson(@case.CaseType.Translations)
                    },
                    Metadata = @case.Metadata,
                    GroupId = @case.GroupId,
                    CheckpointType = new CheckpointType {
                        Id = @case.Checkpoint.CheckpointType.Id,
                        Status = @case.Checkpoint.CheckpointType.Status,
                        Code = @case.Checkpoint.CheckpointType.Code,
                        Title = @case.Checkpoint.CheckpointType.Title ?? @case.Checkpoint.CheckpointType.Code,
                        Description = @case.Checkpoint.CheckpointType.Description,
                        Translations = TranslationDictionary<CheckpointTypeTranslation>.FromJson(@case.Checkpoint.CheckpointType.Translations)
                    },
                    AssignedToName = @case.AssignedTo.Name,
                    Data = options.Filter.IncludeData ? @case.Data : null,
                    AccessLevel = 111
                });
        } else {
            query = (from @case in queryCases
                     join checkpoint in _dbContext.Checkpoints
                        on @case.CheckpointId equals checkpoint.Id

                     let caseAccess = _dbContext.CaseAccessRules.Where(x =>
                                    x.RuleCaseId == @case.Id && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == null &&
                                    ((userId != null && x.MemberUserId == userId.ToString()) ||
                                    (userRoles.Any() && userRoles.Contains(x.MemberRole)) ||
                                    (inputGroupId != null && x.MemberGroupId == inputGroupId))
                                    )
                                    .Select(x => x.AccessLevel)
                                    .FirstOrDefault()

                     let CaseTypeAccess = _dbContext.CaseAccessRules.Where(x =>
                                     x.RuleCaseId == null && x.RuleCaseTypeId == @case.CaseTypeId && x.RuleCheckpointTypeId == null &&
                                     ((userId != null && x.MemberUserId == userId.ToString()) ||
                                     (userRoles.Any() && userRoles.Contains(x.MemberRole)) ||
                                     (inputGroupId != null && x.MemberGroupId == inputGroupId)))
                                    .Select(x => x.AccessLevel)
                                    .FirstOrDefault()
                     let CheckpointIdAccess = _dbContext.CaseAccessRules.Where(x =>
                                    x.RuleCaseId == null && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == checkpoint.CheckpointTypeId &&
                                     ((userId != null && x.MemberUserId == userId.ToString()) ||
                                     (userRoles.Any() && userRoles.Contains(x.MemberRole)) ||
                                     (inputGroupId != null && x.MemberGroupId == inputGroupId)))
                                    .Select(x => x.AccessLevel)
                                    .FirstOrDefault()
                     let caseCheckpointIdAccess = _dbContext.CaseAccessRules.Where(x =>
                                      x.RuleCaseId == @case.Id && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == checkpoint.CheckpointTypeId &&
                                      ((userId != null && x.MemberUserId == userId.ToString()) ||
                                      (userRoles.Any() && userRoles.Contains(x.MemberRole)) ||
                                      (inputGroupId != null && x.MemberGroupId == inputGroupId)))
                                     .Select(x => x.AccessLevel)
                                     .FirstOrDefault()

                     let caseAccessCondition = _dbContext.CaseAccessRules.Where(x =>
                                    x.RuleCaseId == @case.Id && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == null &&
                                      ((userId != null && x.MemberUserId == userId.ToString()) ||
                                      (userRoles.Any() && userRoles.Contains(x.MemberRole)) ||
                                      (inputGroupId != null && x.MemberGroupId == inputGroupId)))
                                    .Select(x => x.AccessLevel)
                                    .Any()
                     let CaseTypeCondition = _dbContext.CaseAccessRules.Where(x =>
                                     x.RuleCaseId == null && x.RuleCaseTypeId == @case.CaseTypeId && x.RuleCheckpointTypeId == null &&
                                     ((userId != null && x.MemberUserId == userId.ToString()) ||
                                     (userRoles.Any() && userRoles.Contains(x.MemberRole)) ||
                                     (inputGroupId != null && x.MemberGroupId == inputGroupId)))
                                    .Select(x => x.AccessLevel)
                                    .Any()
                     let CheckpointIdACondition = _dbContext.CaseAccessRules.Where(x =>
                                    x.RuleCaseId == null && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == checkpoint.CheckpointTypeId &&
                                    ((userId != null && x.MemberUserId == userId.ToString()) ||
                                    (userRoles.Any() && userRoles.Contains(x.MemberRole)) ||
                                    (inputGroupId != null && x.MemberGroupId == inputGroupId)))
                                    .Select(x => x.AccessLevel)
                                    .Any()

                     let caseCheckpointIdCondition = _dbContext.CaseAccessRules.Where(x =>
                                    x.RuleCaseId == @case.Id && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == checkpoint.CheckpointTypeId &&
                                  ((userId != null && x.MemberUserId == userId.ToString()) ||
                                  (userRoles.Any() && userRoles.Contains(x.MemberRole)) ||
                                  (inputGroupId != null && x.MemberGroupId == inputGroupId)))
                                 .Select(x => x.AccessLevel)
                                 .Any()

                     where (caseAccessCondition || CaseTypeCondition || CheckpointIdACondition || caseCheckpointIdCondition)

                     select new CasePartial {
                         Id = @case.Id,
                         ReferenceNumber = @case.ReferenceNumber,
                         CustomerId = @case.Customer.CustomerId,
                         CustomerName = @case.Customer.FirstName + " " + @case.Customer.LastName, // concat like this to enable searching with "contains"
                         CreatedByWhen = @case.CreatedBy.When,
                         CaseType = new CaseTypePartial {
                             Id = @case.CaseType.Id,
                             Code = @case.CaseType.Code,
                             Title = @case.CaseType.Title,
                             Translations = TranslationDictionary<CaseTypeTranslation>.FromJson(@case.CaseType.Translations)
                         },
                         Metadata = @case.Metadata,
                         GroupId = @case.GroupId,
                         CheckpointType = new CheckpointType {
                             Id = @case.Checkpoint.CheckpointType.Id,
                             Status = @case.Checkpoint.CheckpointType.Status,
                             Code = @case.Checkpoint.CheckpointType.Code,
                             Title = @case.Checkpoint.CheckpointType.Title ?? @case.Checkpoint.CheckpointType.Code,
                             Description = @case.Checkpoint.CheckpointType.Description,
                             Translations = TranslationDictionary<CheckpointTypeTranslation>.FromJson(@case.Checkpoint.CheckpointType.Translations)
                         },
                         AssignedToName = @case.AssignedTo.Name,
                         Data = options.Filter.IncludeData ? @case.Data : null,
                         AccessLevel = new List<int> { caseAccess, CaseTypeAccess, CheckpointIdAccess, caseCheckpointIdAccess }.Max()
                     });
        }

        //// TODO: not crazy about this one
        //// if a CaseAuthorizationService down the line wants to 
        //// not allow a user to see the list of case, it throws a ResourceUnauthorizedException
        //// which we catch and return an empty resultset. 
        try {
            query = await _memberAuthorizationProvider.GetCaseMembership(query, user);
        } catch (ResourceUnauthorizedException) {
            return new List<CasePartial>().ToResultSet();
        }

        // filter ReferenceNumbers
        if (options.Filter.ReferenceNumbers.Any()) {
            foreach (var refNumber in options.Filter.ReferenceNumbers) {
                if (!int.TryParse(refNumber.Value, out var value)) {
                    continue;
                }
                query = refNumber.Operator switch {
                    FilterOperator.Eq => query.Where(c => (c.ReferenceNumber ?? 0) == value),
                    FilterOperator.Neq => query.Where(c => (c.ReferenceNumber ?? 0) != value),
                    FilterOperator.Contains => query.Where(c =>
                        c.ReferenceNumber.HasValue && c.ReferenceNumber.ToString().Contains(value.ToString())),
                    _ => query
                };
            }
        }

        // filter CustomerId
        if (options.Filter.CustomerIds.Any()) {
            foreach (var customerId in options.Filter.CustomerIds) {
                query = customerId.Operator switch {
                    FilterOperator.Eq => query.Where(c => c.CustomerId.Equals(customerId.Value)),
                    FilterOperator.Neq => query.Where(c => !c.CustomerId.Equals(customerId.Value)),
                    FilterOperator.Contains => query.Where(c => c.CustomerId.Contains(customerId.Value)),
                    _ => query
                };
            }

        }
        // filter CustomerName
        if (options.Filter.CustomerNames.Any()) {
            foreach (var customerName in options.Filter.CustomerNames) {
                query = customerName.Operator switch {
                    FilterOperator.Eq => query.Where(c =>
                        c.CustomerName.ToLower().Equals(customerName.Value.ToLower())),
                    FilterOperator.Neq => query.Where(c =>
                        !c.CustomerName.ToLower().Equals(customerName.Value.ToLower())),
                    FilterOperator.Contains => query.Where(c =>
                        c.CustomerName.ToLower().Contains(customerName.Value.ToLower())),
                    _ => query
                };
            }
        }

        if (options.Filter.From != null) {
            query = query.Where(c => c.CreatedByWhen >= options.Filter.From.Value.Date);
        }

        if (options.Filter.To != null) {
            query = query.Where(c => c.CreatedByWhen <= options.Filter.To.Value.Date.AddDays(1));
        }

        // filter CaseTypeCodes. You can reach this with an empty array only if you are admin/systemic user
        if (options.Filter.CaseTypeCodes.Any()) {
            // Create a different expression based on the filter operator
            var expressionsEq = options.Filter.CaseTypeCodes
                .Where(x => x.Operator == FilterOperator.Eq)
                .Select(f => (Expression<Func<CasePartial, bool>>)(c => c.CaseType.Code == f.Value))
                .ToList();
            var expressionsNeq = options.Filter.CaseTypeCodes
                .Where(x => x.Operator == FilterOperator.Neq)
                .Select(f => (Expression<Func<CasePartial, bool>>)(c => c.CaseType.Code != f.Value))
                .ToList();
            if (expressionsEq.Any()) {
                // Aggregate the expressions with OR in SQL
                var aggregatedExpressionEq = expressionsEq.Aggregate((expression, next) => {
                    var orExp = Expression.OrElse(expression.Body, Expression.Invoke(next, expression.Parameters));
                    return Expression.Lambda<Func<CasePartial, bool>>(orExp, expression.Parameters);
                });
                query = query.Where(aggregatedExpressionEq);
            }
            if (expressionsNeq.Any()) {
                // Aggregate the expression with AND in SQL
                var aggregatedExpressionNeq = expressionsNeq.Aggregate((expression, next) => {
                    var andExp = Expression.AndAlso(expression.Body, Expression.Invoke(next, expression.Parameters));
                    return Expression.Lambda<Func<CasePartial, bool>>(andExp, expression.Parameters);
                });
                query = query.Where(aggregatedExpressionNeq);
            }
        }

        // if we have Filter.CheckpointTypeCodes from the client, we have to map them to the correct checkpoint types for the filter to work
        if (options.Filter.CheckpointTypeCodes.Any()) {
            options.Filter.CheckpointTypeIds = await MapCheckpointTypeCodeToId(options.Filter.CheckpointTypeCodes);
        }

        // also: filter CheckpointTypeIds
        if (options.Filter.CheckpointTypeIds.Any()) {
            // Create a different expression based on the filter operator
            var expressionsEq = options.Filter.CheckpointTypeIds
                .Where(x => x.Operator == FilterOperator.Eq)
                .Select(f => (Expression<Func<CasePartial, bool>>)(c => c.CheckpointType.Id.ToString() == f.Value))
                .ToList();
            var expressionsNeq = options.Filter.CheckpointTypeIds
                .Where(x => x.Operator == FilterOperator.Neq)
                .Select(f => (Expression<Func<CasePartial, bool>>)(c => c.CheckpointType.Id.ToString() != f.Value))
                .ToList();
            if (expressionsEq.Any()) {
                // Aggregate the expressions with OR in SQL
                var aggregatedExpressionEq = expressionsEq.Aggregate((expression, next) => {
                    var orExp = Expression.OrElse(expression.Body, Expression.Invoke(next, expression.Parameters));
                    return Expression.Lambda<Func<CasePartial, bool>>(orExp, expression.Parameters);
                });
                query = query.Where(aggregatedExpressionEq);
            }
            if (expressionsNeq.Any()) {
                // Aggregate the expression with AND in SQL
                var aggregatedExpressionNeq = expressionsNeq.Aggregate((expression, next) => {
                    var andExp = Expression.AndAlso(expression.Body, Expression.Invoke(next, expression.Parameters));
                    return Expression.Lambda<Func<CasePartial, bool>>(andExp, expression.Parameters);
                });
                query = query.Where(aggregatedExpressionNeq);
            }
        }

        // filter by group ID, if it is present
        if (options.Filter.GroupIds.Any()) {
            foreach (var groupId in options.Filter.GroupIds) {
                query = groupId.Operator switch {
                    FilterOperator.Eq => query.Where(c => c.GroupId.Equals(groupId.Value)),
                    FilterOperator.Neq => query.Where(c => !c.GroupId.Equals(groupId.Value)),
                    FilterOperator.Contains => query.Where(c => c.GroupId.Contains(groupId.Value)),
                    _ => query
                };
            }
        }

        if (options.Filter.Data.Any()) {
            // Execute the query with all the previous filters and 
            // select the case Ids
            var caseIds = (await query.ToListAsync()).Select(x => x.Id);

            // For those Ids, execute a second query to filter the cases by caseData json filter
            var caseData = await _dbContext.CaseData
                .AsNoTracking()
                .Where(options.Filter.Data)
                .Where(x => caseIds.Contains(x.CaseId))
                .Select(x => x.CaseId)
                .ToListAsync();

            // update the initial queryable, to execute (again) but with paging results
            query = query.Where(x => caseData.Contains(x.Id));
        }

        // sorting option
        if (options.Sort is null) {
            options.Sort = $"{nameof(CasePartial.CreatedByWhen)}";
        }

        var result = await query.ToResultSetAsync(options);

        // translate case types
        foreach (var item in result.Items) {
            item.CaseType = item.CaseType?.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
            item.CheckpointType = item.CheckpointType?.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
        }
        return result;
    }

    public async Task<Case> GetCaseById(ClaimsPrincipal user, Guid caseId, bool? includeAttachmentData) {
        var query =
            from c in GetCasesInternal(user.FindSubjectId(), includeAttachmentData ?? false, SchemaKey)
            where c.Id == caseId
            select c;

        var @case = await query.FirstOrDefaultAsync();

        // Check that user role can view this case at this checkpoint.
        if (!await _memberAuthorizationProvider.IsMember(user, @case)) {
            throw new ResourceUnauthorizedException();
        }
        @case.CaseType = @case.CaseType.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
        @case.CheckpointType = @case.CheckpointType.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
        return @case;
    }

    public async Task DeleteDraft(ClaimsPrincipal user, Guid caseId) {
        var @case = await _dbContext.Cases.FindAsync(caseId);
        if (@case is null) {
            throw new CaseNotFoundException();
        }

        if (@case.CreatedBy.Id != user.FindSubjectId()) {
            throw new ResourceUnauthorizedException();
        }

        if (!@case.Draft) {
            throw new Exception("Cannot delete a submitted case.");
        }

        _dbContext.Remove(@case);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<DbAttachment> GetDbAttachmentById(ClaimsPrincipal user, Guid attachmentId) {
        var attachment = await _dbContext.Attachments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == attachmentId);

        // Check that user role can download this attachment.
        await GetCaseById(user, attachment.CaseId, false);

        return attachment;
    }

    public async Task<ResultSet<CaseAttachment>> GetAttachments(Guid caseId) {
        var attachments = await _dbContext.Attachments
            .AsNoTracking()
            .Where(x => x.CaseId == caseId)
            .Select(db => new CaseAttachment {
                Id = db.Id,
                ContentType = db.ContentType,
                Name = db.Name,
                Extension = db.FileExtension
            })
            .ToListAsync();
        return attachments.ToResultSet();
    }

    public async Task<CaseAttachment> GetAttachment(Guid caseId, Guid attachmentId) {
        var attachment = await _dbContext.Attachments
            .AsNoTracking()
            .Select(db => new CaseAttachment {
                Id = db.Id,
                ContentType = db.ContentType,
                Name = db.Name,
                Extension = db.FileExtension,
                Data = db.Data
            })
            .SingleOrDefaultAsync(x => x.Id == attachmentId);
        return attachment;
    }

    public async Task<CaseAttachment> GetAttachmentByField(ClaimsPrincipal user, Guid caseId, string fieldName) {
        var stringifiedCaseData = (await GetCaseById(user, caseId, false)).DataAs<string>();
        var json = JsonDocument.Parse(stringifiedCaseData);
        bool found = json.RootElement.TryGetProperty(fieldName, out JsonElement attachmentId);
        if (found && !string.IsNullOrEmpty(attachmentId.GetString())) {
            var attachment = await GetAttachment(caseId, attachmentId.GetGuid());
            return attachment;
        }
        return null;
    }

    public async Task<bool> PatchCaseMetadata(Guid caseId, ClaimsPrincipal User, Dictionary<string, string> metadata) {
        // Check that user role can view this case
        await GetCaseById(User, caseId, false);

        var dbCase = await _dbContext.Cases.FindAsync(caseId);
        if (dbCase == null) {
            return false;
        }
        foreach (var keyValuePair in metadata) {
            dbCase.Metadata[keyValuePair.Key] = keyValuePair.Value;
        }
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<AuditMeta> AssignCase(AuditMeta user, Guid caseId) {
        if (user.Id == default || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Name)) {
            throw new ArgumentException($"{BasicClaimTypes.GivenName} or {BasicClaimTypes.FamilyName} is missing from identity claim types");
        }
        var @case = await _dbContext.Cases.FindAsync(caseId);
        if (@case == null) {
            throw new ArgumentNullException($"No {nameof(@case)} found with that id");
        }
        if (@case.AssignedTo != null && @case.AssignedTo.Id != user.Id) {
            throw new InvalidOperationException("Case is already assigned to another user.");
        }

        // Apply assignment
        @case.AssignedTo = user;
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public async Task RemoveAssignment(Guid caseId) {
        var @case = await _dbContext.Cases.FindAsync(caseId);
        if (@case == null) {
            throw new ArgumentNullException(nameof(@case));
        }

        @case.AssignedTo = null;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<TimelineEntry>> GetTimeline(ClaimsPrincipal user, Guid caseId) {
        // Check that user role can view this case
        await GetCaseById(user, caseId, false);

        var comments = await _dbContext.Comments
            .AsNoTracking()
            .Where(c => c.CaseId == caseId)
            .ToListAsync();

        var checkpoints = await _dbContext.Checkpoints
            .AsNoTracking()
            .Include(c => c.CheckpointType)
            .Where(c => c.CaseId == caseId)
            .ToListAsync();

        var timeline = comments
            .Select(c => new TimelineEntry {
                Timestamp = c.CreatedBy.When!.Value,
                CreatedBy = c.CreatedBy,
                Comment = new Comment {
                    Id = c.Id,
                    IsCustomer = c.IsCustomer,
                    Attachment = c.AttachmentId.HasValue
                        ? new CasesAttachmentLink {
                            Id = c.AttachmentId.Value
                        }
                        : null,
                    Private = c.Private,
                    //ReplyToComment = null,// todo reply
                    Text = c.Text
                }
            })
            .Concat(checkpoints.Select(c => new TimelineEntry {
                Timestamp = c.CreatedBy.When!.Value,
                CreatedBy = c.CreatedBy,
                Checkpoint = new Checkpoint {
                    Id = c.Id,
                    CheckpointType = new CheckpointType {
                        Id = c.CheckpointType.Id,
                        Code = c.CheckpointType.Code,
                        Description = c.CheckpointType.Description,
                        Translations = TranslationDictionary<CheckpointTypeTranslation>.FromJson(c.CheckpointType.Translations),
                        Private = c.CheckpointType.Private,
                        Status = c.CheckpointType.Status
                    },
                    CompletedDate = c.CompletedDate,
                    DueDate = c.DueDate
                }
            }))
            .OrderByDescending(c => c.Timestamp)
            .ThenBy(c => c.IsCheckpoint)
            .ToList();

        foreach (var timelineEntry in timeline.Where(x => x.Checkpoint is not null)) {
            timelineEntry.Checkpoint.CheckpointType = timelineEntry.Checkpoint.CheckpointType.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
        }

        return timeline;
    }

    public async Task<List<CasePartial>> GetRelatedCases(ClaimsPrincipal user, Guid caseId) {
        // Check that user role can view this case
        var @case = await GetCaseById(user, caseId, false);
        var result = await GetCases(user, new ListOptions<GetCasesListFilter>() {
            Filter = new GetCasesListFilter {
                Metadata = [new FilterClause("metadata.ExternalCorrelationKey", @case.Metadata["ExternalCorrelationKey"], FilterOperator.Eq, JsonDataType.String)]
            }
        });

        return result.Items.OrderByDescending(x=> x.CreatedByWhen).ToList();
    }

    private async Task<List<FilterClause>> MapCheckpointTypeCodeToId(List<FilterClause> checkpointTypeCodeFilterClauses) {
        var checkpointTypeCodes = checkpointTypeCodeFilterClauses.Select(f => f.Value).ToList();
        var checkpointTypeIds = new List<FilterClause>();
        var filteredCheckpointTypesList = await _dbContext.CheckpointTypes
                .AsQueryable()
                .Where(checkpointType => checkpointTypeCodes.Contains(checkpointType.Code))
                .ToListAsync();
        foreach (var checkpointType in filteredCheckpointTypesList) {
            // find the checkpoint type that matches the checkpoint type code given in the parameters
            if (checkpointTypeCodeFilterClauses.Select(f => f.Value).Contains(checkpointType.Code)) {
                var checkpointTypeOperator = checkpointTypeCodeFilterClauses.FirstOrDefault(f => f.Value == checkpointType.Code).Operator;
                // create a new FilterClause object that holds the Id but also the operator
                var newCheckpointTypeIdFilterClause = new FilterClause("checkpointTypeId", checkpointType.Id.ToString(), checkpointTypeOperator, JsonDataType.String);
                checkpointTypeIds.Add(newCheckpointTypeIdFilterClause);
            }
        }
        return checkpointTypeIds;
    }

}