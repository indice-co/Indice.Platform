using System.Security.Claims;
using System.Text.Json;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Extensions;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services;

internal abstract class BaseCaseService
{
    private readonly CasesDbContext _dbContext;
    private readonly CasesApiOptions _options;

    protected BaseCaseService(CasesDbContext dbContext, CasesApiOptions options) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options;
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

    /// <summary>Create a IQueryable from a <see cref="DbCase"/> projected to a <see cref="Case"/>.</summary>
    /// /// <param name="userId">The Id of the user</param>
    /// <param name="includeAttachmentData">Include the attachment binary data to the response.</param>
    /// <param name="schemaKey">The schemaKey for the case type JSON schema/layout. Can be "frontend", "backoffice" or null</param>
    protected IQueryable<Case> GetCasesInternal(string userId, bool includeAttachmentData = false, string schemaKey = null) {
        var query =
            from c in _dbContext.Cases.AsQueryable().AsNoTracking()
            let isCustomer = userId == c.Customer.UserId
            select new Case {
                Id = c.Id,
                ReferenceNumber = c.ReferenceNumber,
                CheckpointType = new CheckpointType {
                    Id = isCustomer ? c.PublicCheckpoint.CheckpointType.Id : c.Checkpoint.CheckpointType.Id,
                    Status = isCustomer ? c.PublicCheckpoint.CheckpointType.Status : c.Checkpoint.CheckpointType.Status,
                    Code = isCustomer ? c.PublicCheckpoint.CheckpointType.Code : c.Checkpoint.CheckpointType.Code,
                    Title = isCustomer ? c.PublicCheckpoint.CheckpointType.Title : c.Checkpoint.CheckpointType.Title,
                    Description = isCustomer ? c.PublicCheckpoint.CheckpointType.Description : c.Checkpoint.CheckpointType.Description,
                    Translations = TranslationDictionary<CheckpointTypeTranslation>.FromJson(
                        isCustomer ? c.PublicCheckpoint.CheckpointType.Translations : c.Checkpoint.CheckpointType.Translations),
                },
                CreatedByWhen = c.CreatedBy.When,
                CreatedById = c.CreatedBy.Id,
                CreatedByEmail = c.CreatedBy.Email,
                CreatedByName = c.CreatedBy.Name,
                CaseType = new CaseTypePartial {
                    Code = c.CaseType.Code,
                    Title = c.CaseType.Title,
                    Id = c.CaseType.Id,
                    DataSchema = GetSingleOrMultiple(schemaKey, c.CaseType.DataSchema),
                    Layout = GetSingleOrMultiple(schemaKey, c.CaseType.Layout),
                    LayoutTranslations = c.CaseType.LayoutTranslations,
                    Translations = TranslationDictionary<CaseTypeTranslation>.FromJson(c.CaseType.Translations),
                    Tags = c.CaseType.Tags,
                    Config = GetSingleOrMultiple(schemaKey, c.CaseType.Config)
                },
                CustomerId = c.Customer.CustomerId,
                CustomerName = c.Customer.FullName,
                UserId = c.Customer.UserId,
                GroupId = c.GroupId,
                Metadata = c.Metadata,
                Attachments = c.Attachments.Select(attachment => new CaseAttachment {
                    Id = attachment.Id,
                    Name = attachment.Name,
                    ContentType = attachment.ContentType,
                    Extension = attachment.FileExtension,
                    Data = includeAttachmentData == true ? attachment.Data : null
                }).ToList(),
                Data = isCustomer ? c.PublicData.Data : c.Data.Data,
                AssignedToName = c.AssignedTo.Name,
                Channel = c.Channel,
                Draft = c.Draft,
                Approvers = c.Approvals
                    .Where(p => p.Committed && p.Action == Approval.Approve)
                    .Select(p => p.CreatedBy)
                    .OrderBy(p => p.When)
                    .ToList(),
                CaseMembers = c.CaseMembers
                        .Select(p => new CaseMember() {
                            MemberId = p.MemberUserId,
                            Accesslevel = p.AccessLevel,
                            CreatedDate = p.CreatedDate
                        })
                        .OrderBy(p => p.CreatedDate)
                        .ToList()
            };
        return query;
    }

    /// <summary>Get the case as requested by a Customer. Case must match <see cref="DbCase.CreatedBy"/> with the <see cref="ClaimsPrincipal"/> of the customer.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="customer">The customer that initiated the request.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    protected async Task<DbCase> GetDbCaseForCustomer(Guid caseId, ClaimsPrincipal customer) {
        var userId = customer.FindSubjectIdOrClientId();
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

        // If enabled get a new reference number from the sequence.
        if (_options.ReferenceNumberEnabled) {
            entity.ReferenceNumber = await _dbContext.GetNextReferenceNumber();
        }

        // Create entity
        await _dbContext.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        // The first checkpoint is always "Submitted" with Draft = true
        await caseMessageService.Send(entity.Id, user, new Message { CheckpointTypeName = "Submitted" });
        return entity;
    }
}