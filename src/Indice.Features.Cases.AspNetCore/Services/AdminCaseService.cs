using System.Globalization;
using System.Security.Claims;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Events;
using Indice.Features.Cases.Exceptions;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
    internal class AdminCaseService : BaseCaseService, IAdminCaseService
    {
        private const string SchemaKey = "backoffice";
        private readonly CasesDbContext _dbContext;
        private readonly ICaseAuthorizationProvider _roleCaseTypeProvider;
        private readonly ICaseTypeService _caseTypeService;
        private readonly IAdminCaseMessageService _adminCaseMessageService;
        private readonly ICaseEventService _caseEventService;

        public AdminCaseService(
            CasesDbContext dbContext,
            ICaseAuthorizationProvider roleCaseTypeProv,
            ICaseTypeService caseTypeService,
            IAdminCaseMessageService adminCaseMessageService,
            ICaseEventService caseEventService) : base(dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _roleCaseTypeProvider = roleCaseTypeProv ?? throw new ArgumentNullException(nameof(roleCaseTypeProv));
            _caseTypeService = caseTypeService ?? throw new ArgumentNullException(nameof(caseTypeService));
            _adminCaseMessageService = adminCaseMessageService ?? throw new ArgumentNullException(nameof(adminCaseMessageService));
            _caseEventService = caseEventService ?? throw new ArgumentNullException(nameof(caseEventService));
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

        public async Task UpdateData(ClaimsPrincipal user, Guid caseId, string data) {
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
            // TODO: not crazy about this one
            // if a CaseAuthorizationService down the line wants to 
            // not allow a user to see the list of case, it throws a ResourceUnauthorizedException
            // which we catch and return an empty resultset. 
            try {
                options.Filter = await _roleCaseTypeProvider.Filter(user, options.Filter);
            } catch (ResourceUnauthorizedException) {
                return new List<CasePartial>().ToResultSet();
            }
            
            var query = _dbContext.Cases
                .AsNoTracking()
                .Where(c => !c.Draft) // filter out draft cases
                .Where(options.Filter.Metadata) // filter Metadata
                .Select(@case => new CasePartial {
                    Id = @case.Id,
                    CustomerId = @case.Customer.CustomerId,
                    CustomerName = @case.Customer.FirstName + " " + @case.Customer.LastName, // concat like this to enable searching with "contains"
                    Status = @case.Checkpoint.CheckpointType.Status,
                    CreatedByWhen = @case.CreatedBy.When,
                    CaseType = new CaseTypePartial {
                        Id = @case.CaseType.Id,
                        Code = @case.CaseType.Code,
                        Title = @case.CaseType.Title,
                        Translations = TranslationDictionary<CaseTypeTranslation>.FromJson(@case.CaseType.Translations)
                    },
                    Metadata = @case.Metadata,
                    GroupId = @case.GroupId,
                    CheckpointTypeId = @case.Checkpoint.CheckpointTypeId,
                    CheckpointTypeCode = @case.Checkpoint.CheckpointType.Code,
                    AssignedToName = @case.AssignedTo.Name
                });

            // filter CustomerId
            if (options.Filter.CustomerId != null) {
                query = query.Where(c => c.CustomerId.ToLower().Contains(options.Filter.CustomerId.ToLower()));
            }
            // filter CustomerName
            if (options.Filter.CustomerName != null) {
                query = query.Where(c => c.CustomerName.ToLower().Contains(options.Filter.CustomerName.ToLower()));
            }
            if (options.Filter.From != null) {
                query = query.Where(c => c.CreatedByWhen >= options.Filter.From.Value.Date);
            }
            if (options.Filter.To != null) {
                query = query.Where(c => c.CreatedByWhen <= options.Filter.To.Value.Date.AddDays(1));
            }
            // filter CaseTypeCodes. You can reach this with an empty array only if you are admin/systemic user
            if (options.Filter.CaseTypeCodes != null && options.Filter.CaseTypeCodes.Any()) {
                query = query.Where(c => options.Filter.CaseTypeCodes.Contains(c.CaseType.Code));
            }
            // also: filter CheckpointTypeIds
            if (options.Filter.CheckpointTypeIds is not null && options.Filter.CheckpointTypeIds.Any()) {
                query = query.Where(c => options.Filter.CheckpointTypeIds.Contains(c.CheckpointTypeId.ToString()));
            }
            // filter by group ID, if it is present
            if (options.Filter.GroupIds != null && options.Filter.GroupIds.Any()) {
                query = query.Where(c => options.Filter.GroupIds.Contains(c.GroupId));
            }
            // sorting option
            if (string.IsNullOrEmpty(options?.Sort)) {
                options!.Sort = $"{nameof(CasePartial.CreatedByWhen)}";
            }
            var result = await query.ToResultSetAsync(options);
            // translate case types
            foreach (var item in result.Items) {
                item.CaseType = item.CaseType?.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
            }
            return result;
        }

        public async Task<CaseDetails> GetCaseById(ClaimsPrincipal user, Guid caseId, bool? includeAttachmentData) {
            var @case = await _dbContext.Cases
                .AsNoTracking()
                .Include(c => c.CaseType)
                .Include(c => c.PublicCheckpoint)
                .Include(c => c.Attachments)
                .Include(c => c.Approvals)
                .SingleOrDefaultAsync(dbCase => dbCase.Id == caseId);

            if (@case is null) {
                return null;
            }

            var caseData = await _dbContext.CaseData
                .AsNoTracking()
                .Where(dbCaseData => dbCaseData.CaseId == caseId)
                .OrderByDescending(c => c.CreatedBy.When)
                .FirstOrDefaultAsync();

            var caseDetails = await GetCaseByIdInternal(@case, caseData, includeAttachmentData, SchemaKey);

            // Check that user role can view this case at this checkpoint.
            if (!await _roleCaseTypeProvider.IsValid(user, caseDetails)) {
                throw new ResourceUnauthorizedException();
            }

            return caseDetails;
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

        public async Task<AuditMeta> AssignCase(ClaimsPrincipal user, Guid caseId) {
            var assignedTo = AuditMeta.Create(user);
            var @case = await _dbContext.Cases.FindAsync(caseId);
            if (@case == null) {
                throw new ArgumentNullException(nameof(@case));
            }

            if (@case.AssignedTo != null && @case.AssignedTo.Id != assignedTo.Id) {
                throw new InvalidOperationException("Case is already assigned to another user.");
            }

            // Apply assignment
            @case.AssignedTo = assignedTo;
            await _dbContext.SaveChangesAsync();
            return assignedTo;
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
                        Private = c.CheckpointType.Private,
                        CheckpointTypeCode = c.CheckpointType.Code,
                        CompletedDate = c.CompletedDate,
                        DueDate = c.DueDate,
                        Status = c.CheckpointType.Status
                    }
                }))
                .OrderByDescending(c => c.Timestamp)
                .ThenBy(c => c.IsCheckpoint);

            return timeline;
        }
    }
}