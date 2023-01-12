using System.Security.Claims;
using System.Text.Json;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
    internal abstract class BaseCaseService
    {
        private readonly CasesDbContext _dbContext;

        protected BaseCaseService(CasesDbContext dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        protected async Task<DbCaseType> GetCaseTypeInternal(string caseTypeCode) {
            return await _dbContext.CaseTypes
                 .AsNoTracking()
                 .FirstOrDefaultAsync(t => t.Code == caseTypeCode);
        }

        /// <summary>
        /// receives a json and either returns the child of "selectorProperty"
        /// or the json itself 
        /// if no multiples are found
        /// </summary>
        /// <param name="selectorProperty"></param>
        /// <param name="json"></param>
        /// <returns>string schema</returns>
        protected static string GetSingleOrMultiple(string selectorProperty, string json) {
            if (!string.IsNullOrEmpty(selectorProperty) && !string.IsNullOrEmpty(json)) {
                using (var document = JsonDocument.Parse(json)) {
                    if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty(selectorProperty, out var node)) {
                        return node.ToString();
                    }
                }
            }
            return json;
        }

        /// <summary>
        /// Transform a <see cref="DbCase"/> to <see cref="CaseDetails"/>. 
        /// </summary>
        /// <param name="case">The <see cref="DbCase"/>.</param>
        /// <param name="caseData">The <see cref="DbCaseData"/>.</param>
        /// <param name="includeAttachmentData">Include the attachment binary data to the response.</param>
        /// <param name="schemaKey">The schemaKey for the case type JSON schema/layout. Can be "frontend", "backoffice" or null</param>
        /// <returns></returns>
        protected async Task<CaseDetails> GetCaseByIdInternal(DbCase @case, DbCaseData caseData, bool? includeAttachmentData = null, string schemaKey = null) {
            // Do the "latestCheckpoint query" here and avoid N triggers when constructing the case details model
            var latestCheckpoint = await _dbContext.Checkpoints
                .Include(c => c.CheckpointType)
                .Where(c => c.CaseId == @case.Id)
                .OrderByDescending(c => c.CreatedBy.When)
                .FirstOrDefaultAsync();

            var caseDetails = new CaseDetails {
                Id = @case.Id,
                Status = latestCheckpoint.CheckpointType.Status,
                CreatedByWhen = @case.CreatedBy.When,
                CreatedById = @case.CreatedBy.Id,
                CreatedByEmail = @case.CreatedBy.Email,
                CreatedByName = @case.CreatedBy.Name,
                CaseType = new CaseTypePartial {
                    Code = @case.CaseType.Code,
                    Title = @case.CaseType.Title,
                    Id = @case.CaseType.Id,
                    DataSchema = GetSingleOrMultiple(schemaKey, @case.CaseType.DataSchema),
                    Layout = GetSingleOrMultiple(schemaKey, @case.CaseType.Layout),
                    Translations = TranslationDictionary<CaseTypeTranslation>.FromJson(@case.CaseType.Translations),
                    Config = @case.CaseType.Config
                },
                CustomerId = @case.Customer.CustomerId,
                CustomerName = @case.Customer.FullName,
                UserId = @case.Customer.UserId,
                GroupId = @case.GroupId,
                Metadata = @case.Metadata,
                CheckpointTypeCode = latestCheckpoint.CheckpointType.Code,
                CheckpointTypeId = latestCheckpoint.CheckpointType.Id,
                Attachments = @case.Attachments.Select(attachment => new CaseAttachment {
                    Id = attachment.Id,
                    Name = attachment.Name,
                    ContentType = attachment.ContentType,
                    Extension = attachment.FileExtension,
                    Data = includeAttachmentData == true ? attachment.Data : null
                }).ToList(),
                Data = caseData?.Data,
                AssignedToName = @case.AssignedTo?.Name,
                Channel = @case.Channel,
                Draft = @case.Draft,
                Approvers = @case.Approvals
                    .Where(p => p.Committed && p.Action == Approval.Approve)
                    .Select(p => p.CreatedBy)
                    .OrderBy(p => p.When)
                    .ToList()
            };

            return caseDetails;
        }

        /// <summary>
        /// Get the case as requested by a Customer. Case must match <see cref="DbCase.CreatedBy"/> with the <see cref="ClaimsPrincipal"/> of the customer.
        /// </summary>
        /// <param name="caseId">The Id of the case.</param>
        /// <param name="customer">The customer that initiated the request.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected async Task<DbCase> GetDbCaseForCustomer(Guid caseId, ClaimsPrincipal customer) {
            var userId = customer.FindSubjectId();
            var @case = await _dbContext.Cases
                .Include(c => c.CaseType)
                .FirstOrDefaultAsync(p => p.Id == caseId && (p.CreatedBy.Id == userId || p.Customer.UserId == userId));
            if (@case == null) {
                throw new Exception("Case not found."); // todo  proper exception & handle from problemConfig (NotFound)
            }
            return @case;
        }

        /// <summary>
        /// Create a new case in draft mode. In draft mode we are creating a default checkpoint "Submitted" where the user is able to add attachments before the
        /// final case submission.
        /// </summary>
        /// <param name="caseMessageService">The message service of the caller.</param>
        /// <param name="user">The user that initiated the case.</param>
        /// <param name="caseType">The case type</param>
        /// <param name="groupId">The id to group the case (eg in banking business it can be the BranchId of the customer).</param>
        /// <param name="customer">The customer metadata that initiated the case.</param>
        /// <param name="metadata">The metadata the case might have.</param>
        /// <param name="channel">The channel the case was created from.</param>
        /// <param name="assignee">The assigned of the case (optional).</param>
        /// <returns></returns>
        protected async Task<DbCase> CreateDraftInternal(
            ICaseMessageService caseMessageService,
            ClaimsPrincipal user,
            DbCaseType caseType,
            string groupId,
            CustomerMeta customer,
            Dictionary<string, string> metadata,
            string channel,
            ClaimsPrincipal assignee = null) {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (caseType == null) throw new ArgumentNullException(nameof(caseType));
            if (string.IsNullOrEmpty(channel)) throw new ArgumentNullException(nameof(channel));

            var userMeta = AuditMeta.Create(user);
            var entity = new DbCase {
                Id = Guid.NewGuid(),
                CaseType = caseType,
                CaseTypeId = caseType.Id,
                Priority = Priority.Normal,
                GroupId = groupId,
                Customer = customer,
                CreatedBy = userMeta,
                Metadata = metadata,
                Draft = true,
                Channel = channel,
                AssignedTo = assignee == null ? null : AuditMeta.Create(assignee)
            };

            // Create entity
            await _dbContext.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            // The first checkpoint is always "Submitted" with Draft = true
            await caseMessageService.Send(entity.Id, user, new Message { CheckpointTypeName = "Submitted" });
            return entity;
        }
    }
}