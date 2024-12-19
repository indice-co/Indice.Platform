using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Services;

internal abstract class BaseCaseService
{
    protected CasesDbContext DbContext { get; }
    protected CasesOptions Options { get; }

    protected BaseCaseService(CasesDbContext dbContext, IOptions<CasesOptions> options) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        Options = options.Value;
    }

    protected async Task<DbCaseType?> GetCaseTypeInternal(string caseTypeCode) {
        return await DbContext.CaseTypes
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
    protected static JsonNode? GetSingleOrMultiple(string? selectorProperty, JsonNode? json) {
        if (!string.IsNullOrEmpty(selectorProperty) && json is not null) {
            if (json.GetValueKind() == JsonValueKind.Object && json[selectorProperty!] is not null ) {
                return json[selectorProperty!];
            }
        }
        return json;
    }

    /// <summary>Create a IQueryable from a <see cref="DbCase"/> projected to a <see cref="Case"/>.</summary>
    /// /// <param name="userId">The Id of the user</param>
    /// <param name="includeAttachmentData">Include the attachment binary data to the response.</param>
    /// <param name="schemaKey">The schemaKey for the case type JSON schema/layout. Can be "frontend", "backoffice" or null</param>
    protected IQueryable<Case> GetCasesInternal(string userId, bool includeAttachmentData = false, string? schemaKey = null) {
        var query =
            from c in DbContext.Cases.AsQueryable().AsNoTracking()
            let isCustomer = userId == c.Owner.UserId
            select new Case {
                Id = c.Id,
                ReferenceNumber = c.ReferenceNumber,
                CheckpointType = new CheckpointType {
                    Id = isCustomer ? c.PublicCheckpoint.CheckpointType.Id : c.Checkpoint.CheckpointType.Id,
                    Status = isCustomer ? c.PublicCheckpoint.CheckpointType.Status : c.Checkpoint.CheckpointType.Status,
                    Code = isCustomer ? c.PublicCheckpoint.CheckpointType.Code : c.Checkpoint.CheckpointType.Code,
                    Title = isCustomer ? c.PublicCheckpoint.CheckpointType.Title : c.Checkpoint.CheckpointType.Title,
                    Description = isCustomer ? c.PublicCheckpoint.CheckpointType.Description : c.Checkpoint.CheckpointType.Description,
                    Translations = 
                        isCustomer ? c.PublicCheckpoint.CheckpointType.Translations : c.Checkpoint.CheckpointType.Translations,
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
                    Translations = c.CaseType.Translations,
                    Tags = c.CaseType.Tags,
                    Config = GetSingleOrMultiple(schemaKey, c.CaseType.Config)
                },
                CustomerId = c.Owner.Reference,
                CustomerName = c.Owner.FullName,
                UserId = c.Owner.UserId,
                GroupId = c.GroupId,
                Metadata = c.Metadata,
                Attachments = c.Attachments.Select(attachment => new CaseAttachment {
                    Id = attachment.Id,
                    FileName = attachment.Name,
                    ContentType = attachment.ContentType,
                    FileExtension = attachment.FileExtension,
                    Data = includeAttachmentData == true ? attachment.Data : null
                }).ToList(),
                Data = isCustomer ? c.PublicData.Data : c.Data.Data,
                AssignedToName = c.AssignedTo!.Name,
                Channel = c.Channel,
                Draft = c.Draft,
                Approvers = c.Approvals
                    .Where(p => p.Committed && p.Action == Approval.Approve)
                    .Select(p => p.CreatedBy)
                    .OrderBy(p => p.When)
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
        var @case = await DbContext.Cases
            .Include(c => c.CaseType)
            .FirstOrDefaultAsync(p => p.Id == caseId && (p.CreatedBy.Id == userId || p.Owner.UserId == userId));
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
        string? groupId,
        ContactMeta? customer,
        Dictionary<string, string> metadata,
        string channel,
        ClaimsPrincipal? assignee = null) {
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
            Owner = customer ?? new(),
            CreatedBy = userMeta,
            Metadata = metadata,
            Draft = true,
            Channel = channel,
            AssignedTo = assignee == null ? null : AuditMeta.Create(assignee)
        };

        // If enabled get a new reference number from the sequence.
        if (Options.ReferenceNumberEnabled) {
            entity.ReferenceNumber = await DbContext.GetNextReferenceNumber();
        }

        // Create entity
        await DbContext.AddAsync(entity);
        await DbContext.SaveChangesAsync();

        // The first checkpoint is always "Submitted" with Draft = true
        await caseMessageService.Send(entity.Id, user, new Message { CheckpointTypeName = "Submitted" });
        return entity;
    }
}