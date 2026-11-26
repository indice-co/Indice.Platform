using System.Security.Claims;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>The services regarding Customer's perspective on case management.</summary>
public interface IMyCaseService
{
    /// <summary>
    /// Create a new case in draft mode. In draft mode we are creating a default checkpoint "Submitted" where the user is able to add attachments before the
    /// final case submission.
    /// </summary>
    /// <param name="user">The user that initiated the case.</param>
    /// <param name="caseTypeCode">The case type code.</param>
    /// <param name="groupId">The id to group the case (eg in banking business it can be the BranchId of the customer).</param>
    /// <param name="customer">The customer metadata that initiated the case.</param>
    /// <param name="metadata">The metadata the case might have.</param>
    /// <param name="channel">The channel the case was created from.</param>
    /// <param name="referenceNumber">The reference number the case might have.</param>
    /// <param name="relationKey">The key that correlates the case with other cases. Falls back to the <c>ExternalCorrelationKey</c> metadata entry and is stored there too.</param>
    Task<CreateCaseResponse> CreateDraft(UserActor user, string caseTypeCode, string? groupId, ContactMeta? customer, Dictionary<string, string> metadata, string? channel, string? referenceNumber, string? relationKey = null);

    /// <summary>Update the case with the case data and does a json instance-schema validation of the case type's schema (<see cref="CaseType.DataSchema"/>).</summary>
    /// <param name="user">The user that will update the case.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="data">The case data (as defined by JSON Schema in CaseType).</param>
    Task UpdateData(UserActor user, Guid caseId, dynamic data);

    /// <summary>Submit the case. Case must be in <strong>Case.Draft</strong> mode.</summary>
    /// <param name="user">The user that initiated the submission.</param>
    /// <param name="caseId">The Id of the case.</param>
    Task Submit(UserActor user, Guid caseId);

    /// <summary>Deletes an owned draft case. Returns false if the case does not exist.</summary>
    /// <param name="user">The customer requesting deletion.</param>
    /// <param name="caseId">The case identifier.</param>
    Task<bool> DeleteDraft(UserActor user, Guid caseId);

    /// <summary>Get <see cref="Case"/> for a user by its Id.</summary>
    /// <param name="caseId">The Id of the case.</param>
    Task<Case?> GetCaseById(Guid caseId);

    /// <summary>Get a case with optional inline attachment content. The caller must authorize case access.</summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="includeAttachmentData">Whether to include attachment content.</param>
    async Task<Case?> GetCaseById(Guid caseId, bool includeAttachmentData) {
        var result = await GetCaseById(caseId);
        if (!includeAttachmentData && result?.Attachments is not null) {
            foreach (var attachment in result.Attachments) {
                attachment.Data = null;
            }
        }
        return result;
    }

    /// <summary>Get an attachment belonging to the case. The caller must authorize case access.</summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="attachmentId">The attachment identifier.</param>
    Task<CaseAttachment?> GetAttachment(Guid caseId, Guid attachmentId) =>
        throw new NotSupportedException("This case service does not support attachment downloads.");

    /// <summary>Returns public timeline entries, or null if the case does not exist. The caller must authorize case access.</summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="user">The current customer, whose own audit information is preserved.</param>
    /// <param name="filter">The types of entries to return.</param>
    Task<List<MyTimelineEntry>?> GetTimeline(Guid caseId, UserActor user, TimelineFilter filter = TimelineFilter.All);

    /// <summary>Get the cases of the User.</summary>
    /// <param name="user">The Id of the user to retrieve the cases.</param>
    /// <param name="options">The user list options.</param>
    Task<ResultSet<MyCasePartial>> GetCases(UserActor user, ListOptions<GetMyCasesListFilter> options);

    /// <summary>Gets a case type</summary>
    /// <param name="caseTypeCode">The case type code</param>
    Task<CaseTypePartial> GetCaseType(string caseTypeCode);

    /// <summary>Get case types list.</summary>
    /// <param name="options">The <see cref="GetMyCaseTypesListFilter"/> options.</param>
    Task<ResultSet<CaseTypePartial>> GetCaseTypes(ListOptions<GetMyCaseTypesListFilter> options);

    /// <summary>
    /// Gets the co-owners of a case, without the owner and without their Tax identification number or reference.
    /// The caller must authorize case access. Co-owners are added and removed by the back-office only.
    /// </summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>The co-owners, empty when the case has none or does not exist.</returns>
    Task<ResultSet<MyCaseCoOwner>> GetCoOwners(Guid caseId);
}
