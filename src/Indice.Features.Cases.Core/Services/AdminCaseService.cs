using System.Globalization;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Indice.Events;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Events;
using Indice.Features.Cases.Core.Exceptions;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Indice.Types;
using Json.Patch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Services;

internal class AdminCaseService : BaseCaseService, IAdminCaseService
{
    private const string SchemaKey = "backoffice";
    private readonly ICaseAuthorizationProvider _memberAuthorizationProvider;
    private readonly IAdminCaseMessageService _adminCaseMessageService;
    private readonly IPlatformEventService _platformEventService;
    public AdminCaseService(
        CasesDbContext dbContext,
        IOptions<CasesOptions> options,
        ICaseAuthorizationProvider memberAuthorizationProvider,
        IAdminCaseMessageService adminCaseMessageService,
        IPlatformEventService platformEventService) : base(dbContext, options) {
        _memberAuthorizationProvider = memberAuthorizationProvider ?? throw new ArgumentNullException(nameof(memberAuthorizationProvider));
        _adminCaseMessageService = adminCaseMessageService ?? throw new ArgumentNullException(nameof(adminCaseMessageService));
        _platformEventService = platformEventService ?? throw new ArgumentNullException(nameof(platformEventService));
    }

    public async Task<CreateCaseResponse> CreateDraft(UserActor user,
        string caseTypeCode,
        string? groupId,
        ContactMeta? customer,
        Dictionary<string, string> metadata) {
        var caseType = await DbContext.CaseTypes.Where(x => x.Code == caseTypeCode).SingleAsync();
        var entity = await CreateDraftInternal(
            _adminCaseMessageService,
            user,
            caseType,
            groupId,
            customer,
            metadata,
            CasesCoreConstants.Channels.Agent,
            user);
        return new() { Id = entity.Id, Created = DateTimeOffset.UtcNow };
    }

    public async Task UpdateData(UserActor user, Guid caseId, JsonNode data) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, default);
        ArgumentNullException.ThrowIfNull(data);
        await _adminCaseMessageService.Send(caseId, user, new Message { Data = data });
    }

    /// <summary>
    /// Patches the <strong>CaseData.Data</strong> with the provided JsonNode performing add, replace and remove operations
    /// </summary>
    /// <remarks>For example usages see https://indice.visualstudio.com/Platform/_wiki/wikis/Platform.wiki/1613/Patch-Case-Data-API.</remarks>
    public async Task PatchCaseData(UserActor user, Guid caseId, JsonNode patch, bool patchPublicData) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, default);
        var caseData = (await GetCaseById(caseId, patchPublicData, false)).DataAsJsonNode();

        await _adminCaseMessageService.Send(caseId, user, new Message { Data = caseData.Merge(patch) });
    }

    /// <summary>
    /// Patches the Case Data with list of JsonPatch operations adhering to
    /// https://datatracker.ietf.org/doc/html/rfc6902#appendix-A
    /// </summary>
    /// <remarks>For example usages see https://indice.visualstudio.com/Platform/_wiki/wikis/Platform.wiki/1613/Patch-Case-Data-API.</remarks>
    public async Task PatchCaseData(UserActor user, Guid caseId, JsonPatch operations, bool patchPublicData) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, default);
        var caseData = (await GetCaseById(caseId, patchPublicData, false)).DataAsJsonNode();

        var patchResult = operations.Apply(caseData);
        if (!patchResult.IsSuccess) {
            throw new InvalidOperationException($"Could not apply JSON Patch with error: {patchResult.Error}");
        }

        await _adminCaseMessageService.Send(caseId, user, new Message { Data = patchResult.Result });
    }

    public async Task Submit(UserActor user, Guid caseId) {
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, default);

        var @case = await DbContext
            .Cases
            .Include(c => c.CaseType)
            .FirstOrDefaultAsync(c => c.Id == caseId);
        if (@case == null) {
            throw new ArgumentNullException(nameof(caseId), @"Case does not exist.");
        }
        if (!@case.Draft) {
            throw new BusinessException("Case is submitted."); // todo proper exception (BadRequest)
        }

        @case.Draft = false;
        await DbContext.SaveChangesAsync();

        await _platformEventService.Publish(new CaseSubmittedEvent(new Case {
            Id = @case.Id,
            // TODO: do a proper caseDb to case mapping
        }, @case.CaseType.Code,
        new UserActor {
            Id = @case!.CreatedBy!.Id!,
            Reference = @case.Owner.Reference,
            GroupId = user.GroupId,
            Name = @case.CreatedBy.Name,
            Tin = user.Tin,
            Email = @case.CreatedBy.Email,
            CurrentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
        }));
    }

    public async Task<ResultSet<CasePartial>> GetCases(UserActor user, ListOptions<GetCasesListFilter> options) {

        options.Filter.ShowAll ??= Options.ByPassAccessRulesForElevatedUsers;

        // if client is systemic or admin, then bypass access rule checks since no filtering is required.
        //TODO: this should be a handler method
        var canOverrideAccessRules = user.IsSystemClient || user.IsAdmin;
        //by default we use access rules
        var ignoreAccessRules = canOverrideAccessRules && options.Filter.ShowAll == true;

        var userId = user.Id;
        var inputGroupId = user.GroupId;
        var userRoles = user.Roles;

        var queryCases = DbContext.Cases
            .AsNoTracking()
            .Where(c => !c.Draft) // filter out draft cases
            .Where(options.Filter.Metadata ?? []); // filter Metadata

        // filter assignedTo
        if (!string.IsNullOrWhiteSpace(options.Filter.AssignedTo)) {
            queryCases = queryCases.Where(c => c.AssignedTo.Id == options.Filter.AssignedTo);
        }

        IQueryable<CasePartial> query;
        if (ignoreAccessRules) {
            query = queryCases
                .Select(@case => new CasePartial {
                    Id = @case.Id,
                    ReferenceNumber = @case.ReferenceNumber,
                    OwnerId = @case.Owner.Reference,
                    OwnerName = @case.Owner.FirstName + " " + @case.Owner.LastName, // concat like this to enable searching with "contains"
                    OwnerTin = @case.Owner.Tin,
                    CreatedById = @case.CreatedBy.Id,
                    CreatedByName = @case.CreatedBy.Name,
                    CreatedByEmail = @case.CreatedBy.Email,
                    CreatedByWhen = @case.CreatedBy.When,
                    CaseType = new CaseTypePartial {
                        Id = @case.CaseType.Id,
                        Code = @case.CaseType.Code,
                        Title = @case.CaseType.Title,
                        Translations = @case.CaseType.Translations
                    },
                    Metadata = @case.Metadata,
                    GroupId = @case.GroupId,
                    CheckpointType = new CheckpointType {
                        Id = @case.Checkpoint.CheckpointType.Id,
                        Status = @case.Checkpoint.CheckpointType.Status,
                        Code = @case.Checkpoint.CheckpointType.Code,
                        Title = @case.Checkpoint.CheckpointType.Title ?? @case.Checkpoint.CheckpointType.Code,
                        Description = @case.Checkpoint.CheckpointType.Description,
                        Translations = @case.Checkpoint.CheckpointType.Translations
                    },
                    AssignedToName = @case.AssignedTo!.Name,
                    Data = options.Filter.IncludeData == true ? @case.Data.Data : null,
                    AccessLevel = 111
                });
        } else {
            var accessMatch = Data.Models.DbCaseAccessRule.AccessMatchPredicate(userId, userRoles, inputGroupId);
            query = (from @case in queryCases
                     join checkpoint in DbContext.Checkpoints
                        on @case.CheckpointId equals checkpoint.Id

                     let caseAccess = DbContext.CaseAccessRules
                                    .Where(accessMatch)
                                    .Where(x => x.RuleCaseId == @case.Id && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == null)
                                    .Select(x => x.AccessLevel)
                                    .FirstOrDefault()

                     let CaseTypeAccess = DbContext.CaseAccessRules
                                    .Where(accessMatch)
                                    .Where(x => x.RuleCaseId == null && x.RuleCaseTypeId == @case.CaseTypeId && x.RuleCheckpointTypeId == null)
                                    .Select(x => x.AccessLevel)
                                    .FirstOrDefault()

                     let CheckpointIdAccess = DbContext.CaseAccessRules
                                    .Where(accessMatch)
                                    .Where(x => x.RuleCaseId == null && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == checkpoint.CheckpointTypeId)
                                    .Select(x => x.AccessLevel)
                                    .FirstOrDefault()

                     let caseCheckpointIdAccess = DbContext.CaseAccessRules
                                    .Where(accessMatch)
                                    .Where(x => x.RuleCaseId == @case.Id && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == checkpoint.CheckpointTypeId)
                                    .Select(x => x.AccessLevel)
                                    .FirstOrDefault()

                     let caseAccessCondition = DbContext.CaseAccessRules
                                    .Where(accessMatch)
                                    .Where(x => x.RuleCaseId == @case.Id && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == null)
                                    .Select(x => x.AccessLevel)
                                    .Any()
                     let CaseTypeCondition = DbContext.CaseAccessRules
                                    .Where(accessMatch)
                                    .Where(x => x.RuleCaseId == null && x.RuleCaseTypeId == @case.CaseTypeId && x.RuleCheckpointTypeId == null)
                                    .Select(x => x.AccessLevel)
                                    .Any()
                     let CheckpointIdACondition = DbContext.CaseAccessRules
                                    .Where(accessMatch)
                                    .Where(x => x.RuleCaseId == null && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == checkpoint.CheckpointTypeId)
                                    .Select(x => x.AccessLevel)
                                    .Any()
                     let caseCheckpointIdCondition = DbContext.CaseAccessRules
                                    .Where(accessMatch)
                                    .Where(x => x.RuleCaseId == @case.Id && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == checkpoint.CheckpointTypeId)
                                    .Select(x => x.AccessLevel)
                                    .Any()
                     where (caseAccessCondition || CaseTypeCondition || CheckpointIdACondition || caseCheckpointIdCondition)

                     select new CasePartial {
                         Id = @case.Id,
                         ReferenceNumber = @case.ReferenceNumber,
                         OwnerId = @case.Owner.Reference,
                         OwnerName = @case.Owner.FirstName + " " + @case.Owner.LastName, // concat like this to enable searching with "contains"
                         OwnerTin = @case.Owner.Tin,
                         CreatedById = @case.CreatedBy.Id,
                         CreatedByName = @case.CreatedBy.Name,
                         CreatedByEmail = @case.CreatedBy.Email,
                         CreatedByWhen = @case.CreatedBy.When,
                         CaseType = new CaseTypePartial {
                             Id = @case.CaseType.Id,
                             Code = @case.CaseType.Code,
                             Title = @case.CaseType.Title,
                             Translations = @case.CaseType.Translations
                         },
                         Metadata = @case.Metadata,
                         GroupId = @case.GroupId,
                         CheckpointType = new CheckpointType {
                             Id = @case.Checkpoint.CheckpointType.Id,
                             Status = @case.Checkpoint.CheckpointType.Status,
                             Code = @case.Checkpoint.CheckpointType.Code,
                             Title = @case.Checkpoint.CheckpointType.Title ?? @case.Checkpoint.CheckpointType.Code,
                             Description = @case.Checkpoint.CheckpointType.Description,
                             Translations = @case.Checkpoint.CheckpointType.Translations
                         },
                         AssignedToName = @case.AssignedTo!.Name,
                         Data = options.Filter.IncludeData == true ? @case.Data.Data : null,
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
        if (options.Filter.ReferenceNumbers?.Length > 0) {
            foreach (var refNumber in options.Filter.ReferenceNumbers) {
                if (!int.TryParse(refNumber.Value, out var value)) {
                    continue;
                }
                query = refNumber.Operator switch {
                    FilterOperator.Eq => query.Where(c => (c.ReferenceNumber ?? 0) == value),
                    FilterOperator.Neq => query.Where(c => (c.ReferenceNumber ?? 0) != value),
                    FilterOperator.Contains => query.Where(c =>
                        c.ReferenceNumber.HasValue && c.ReferenceNumber.Value.ToString().Contains(value.ToString())),
                    _ => query
                };
            }
        }



        // filter CustomerId
        if (options.Filter.OwnerIds?.Length > 0) {
            foreach (var ownerId in options.Filter.OwnerIds) {
                query = ownerId.Operator switch {
                    FilterOperator.Eq => query.Where(c => c.OwnerId!.Equals(ownerId.Value)),
                    FilterOperator.Neq => query.Where(c => !c.OwnerId!.Equals(ownerId.Value)),
                    FilterOperator.Contains => query.Where(c => c.OwnerId!.Contains(ownerId.Value)),
                    _ => query
                };
            }

        }
        // filter CustomerName
        if (options.Filter.OwnerNames?.Length > 0) {
            foreach (var customerName in options.Filter.OwnerNames) {
                query = customerName.Operator switch {
                    FilterOperator.Eq => query.Where(c =>
                        c.OwnerName!.ToLower().Equals(customerName.Value.ToLower())),
                    FilterOperator.Neq => query.Where(c =>
                        !c.OwnerName!.ToLower().Equals(customerName.Value.ToLower())),
                    FilterOperator.Contains => query.Where(c =>
                        c.OwnerName!.ToLower().Contains(customerName.Value.ToLower())),
                    _ => query
                };
            }
        }
        // filter Tax Identification Number
        if (options.Filter.OwnerTins?.Length > 0) {
            foreach (var ownerTin in options.Filter.OwnerTins) {
                query = ownerTin.Operator switch {
                    FilterOperator.Eq => query.Where(c =>
                        c.OwnerTin!.ToLower().Equals(ownerTin.Value.ToLower())),
                    FilterOperator.Neq => query.Where(c =>
                        !c.OwnerTin!.ToLower().Equals(ownerTin.Value.ToLower())),
                    FilterOperator.Contains => query.Where(c =>
                        c.OwnerTin!.ToLower().Contains(ownerTin.Value.ToLower())),
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
        if (options.Filter.CaseTypeCodes?.Length > 0) {
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
        if (options.Filter.CheckpointTypeCodes?.Length > 0) {
            options.Filter.CheckpointTypeIds = await MapCheckpointTypeCodeToId(options.Filter.CheckpointTypeCodes);
        }

        // also: filter CheckpointTypeIds
        if (options.Filter.CheckpointTypeIds?.Length > 0) {
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
        if (options.Filter.GroupIds?.Length > 0) {
            foreach (var groupId in options.Filter.GroupIds) {
                query = groupId.Operator switch {
                    FilterOperator.Eq => query.Where(c => c.GroupId!.Equals(groupId.Value)),
                    FilterOperator.Neq => query.Where(c => !c.GroupId!.Equals(groupId.Value)),
                    FilterOperator.Contains => query.Where(c => c.GroupId!.Contains(groupId.Value)),
                    _ => query
                };
            }
        }

        if (options.Filter.Data?.Length > 0) {
            // Execute the query with all the previous filters and 
            // select the case Ids
            var caseIds = (await query.ToListAsync()).Select(x => x.Id);

            // For those Ids, execute a second query to filter the cases by caseData json filter
            var caseData = await DbContext.CaseData
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
            item.CaseType = item.CaseType?.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true)!;
            item.CheckpointType = item.CheckpointType?.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true)!;
        }
        return result;
    }

    public async Task<Case> GetCaseById(Guid caseId, bool fetchPublicData, bool? includeAttachmentData = null) {
        var query =
            from c in GetCasesInternal(fetchPublicData, includeAttachmentData ?? false, SchemaKey)
            where c.Id == caseId
            select c;

        var @case = await query.FirstOrDefaultAsync();
        @case!.CaseType = @case.CaseType.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
        @case.CheckpointType = @case.CheckpointType.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
        return @case;
    }

    public async Task DeleteDraft(UserActor user, Guid caseId) {
        var @case = await DbContext.Cases.FindAsync(caseId);
        if (@case is null) {
            throw new CaseNotFoundException();
        }

        if (@case.CreatedBy.Id != user.Id) {
            throw new ResourceUnauthorizedException();
        }

        if (!@case.Draft) {
            throw new BusinessException("Cannot delete a submitted case.");
        }

        DbContext.Remove(@case);
        await DbContext.SaveChangesAsync();
    }

    public async Task<CaseAttachment?> GetAttachmentById(Guid attachmentId) {
        var attachment = await DbContext.Attachments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == attachmentId);
        if (attachment is null) {
            return null;
        }

        return new CaseAttachment {
            Id = attachmentId,
            ContentType = attachment.ContentType,
            Data = attachment.Data,
            FileExtension = attachment.FileExtension,
            FileName = attachment.Name
        };
    }

    public async Task<ResultSet<CaseAttachment>> GetAttachments(Guid caseId) {
        var attachments = await DbContext.Attachments
            .AsNoTracking()
            .Where(x => x.CaseId == caseId)
            .Select(db => new CaseAttachment {
                Id = db.Id,
                ContentType = db.ContentType,
                FileName = db.Name,
                FileExtension = db.FileExtension
            })
            .ToListAsync();
        return attachments.ToResultSet();
    }

    public async Task<CaseAttachment?> GetAttachment(Guid caseId, Guid attachmentId) {
        var attachment = await DbContext.Attachments
            .AsNoTracking()
            .Where(a => a.CaseId == caseId)
            .Select(db => new CaseAttachment {
                Id = db.Id,
                ContentType = db.ContentType,
                FileName = db.Name,
                FileExtension = db.FileExtension,
                Data = db.Data
            })
            .SingleOrDefaultAsync(x => x.Id == attachmentId);
        return attachment;
    }

    public async Task<CaseAttachment?> GetAttachmentByField(UserActor user, Guid caseId, string fieldName) {
        var json = (await GetCaseById(caseId, false, false)).DataAsJsonNode();
        if (json is null) return null;
        var attachmentId = json[fieldName]?.GetValue<Guid?>();
        if (attachmentId.HasValue) {
            var attachment = await GetAttachment(caseId, attachmentId.Value);
            return attachment;
        }
        return null;
    }

    public async Task<bool> PatchCaseMetadata(Guid caseId, Dictionary<string, string> metadata) {
        var dbCase = await DbContext.Cases.FindAsync(caseId);
        if (dbCase == null) {
            return false;
        }
        if (metadata != null && metadata.Count > 0 && dbCase.Metadata == null) {
            dbCase.Metadata = [];
        }
        foreach (var keyValuePair in metadata!) {
            dbCase.Metadata![keyValuePair.Key] = keyValuePair.Value;
        }
        await DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<AuditMeta> AssignCase(AuditMeta assignTo, Guid caseId) {
        if (assignTo.Id == null || string.IsNullOrEmpty(assignTo.Email) || string.IsNullOrEmpty(assignTo.Name)) {
            throw new ArgumentException($"{BasicClaimTypes.GivenName} or {BasicClaimTypes.FamilyName} is missing from identity claim types");
        }
        var @case = await DbContext.Cases.FindAsync(caseId);
        if (@case == null) {
            throw new ArgumentNullException($"No {nameof(@case)} found with that id");
        }
        if (@case.AssignedTo != null && @case.AssignedTo.Id != assignTo.Id) {
            throw new InvalidOperationException("Case is already assigned to another user.");
        }

        // Apply assignment
        @case.AssignedTo = assignTo;
        await DbContext.SaveChangesAsync();
        return assignTo;
    }

    public async Task RemoveAssignment(Guid caseId) {
        var @case = await DbContext.Cases.FindAsync(caseId);
        if (@case == null) {
            throw new ArgumentNullException(nameof(@case));
        }

        @case.AssignedTo = null;
        await DbContext.SaveChangesAsync();
    }

    public async Task<List<TimelineEntry>> GetTimeline(Guid caseId) {

        var comments = await DbContext.Comments
            .AsNoTracking()
            .Where(c => c.CaseId == caseId)
            .ToListAsync();

        var checkpoints = await DbContext.Checkpoints
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
                    Text = c.Text!
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
                        Translations = c.CheckpointType.Translations,
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
            timelineEntry.Checkpoint!.CheckpointType = timelineEntry.Checkpoint.CheckpointType.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
        }

        return timeline;
    }

    public async Task<List<CasePartial>> GetRelatedCases(UserActor user, Guid caseId) {
        // Check that user role can view this case
        var @case = await GetCaseById(caseId, false, false);
        var result = await GetCases(user, new ListOptions<GetCasesListFilter>() {
            Filter = new GetCasesListFilter {
                Metadata = [new FilterClause("metadata.ExternalCorrelationKey", @case.Metadata!["ExternalCorrelationKey"], FilterOperator.Eq, JsonDataType.String)]
            },
            Sort = $"{nameof(CasePartial.CreatedByWhen)}-"
        });

        return result.Items.Where(x => x.Id != caseId).ToList();
    }

    private async Task<FilterClause[]> MapCheckpointTypeCodeToId(FilterClause[] checkpointTypeCodeFilterClauses) {
        var checkpointTypeCodes = checkpointTypeCodeFilterClauses.Select(f => f.Value).ToList();
        var checkpointTypeIds = new List<FilterClause>();
        var filteredCheckpointTypesList = await DbContext.CheckpointTypes
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
        return [.. checkpointTypeIds];
    }

    public async Task<bool> PublishPrivateData(Guid caseId) {
        ArgumentNullException.ThrowIfNull(caseId);
        var @case = await DbContext.Cases.FirstOrDefaultAsync(p => p.Id == caseId);
        if (@case is null) {
            return false;
        }
        @case.PublicDataId = @case.DataId;
        DbContext.Cases.Update(@case);
        await DbContext.SaveChangesAsync();
        return true;
    }
}