using System.Security.Claims;
using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Types;
using Json.Patch;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>The admin/manage service regarding a Case and its related domain models.</summary>
public interface IAdminCaseService
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
    /// <returns></returns>
    Task<CreateCaseResponse> CreateDraft(ClaimsPrincipal user, string caseTypeCode, string? groupId, ContactMeta? customer, Dictionary<string, string> metadata);

    /// <summary>Update the case with the case data and does a json instance-schema validation of the case type's schema (<see cref="CaseType.DataSchema"/>).</summary>
    /// <param name="user">The user that will update the case.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="data">The case data (as defined by JSON Schema in CaseType).</param>
    /// <returns>A <see cref="Task"/> representing an asynchronous operation</returns>
    Task UpdateData(ClaimsPrincipal user, Guid caseId, JsonNode data);

    /// <summary>
    /// Performs a Partial Upgrade of the case data and does a json instance-schema validation of
    /// the case type's schema (<see cref="CaseType.DataSchema"/>).
    /// `Patch` can be any object e.g. JsonElement, JsonNode, JObject or JToken that can be (de)serialized as Json.
    /// https://indice.visualstudio.com/Platform/_wiki/wikis/Platform.wiki/1613/Patch-Case-Data-API
    /// </summary>
    /// <param name="user">The user that will update the case.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="patch">A JsonNode to merge with the existing case data.</param>
    /// <returns>A <see cref="Task"/> representing an asynchronous operation</returns>
    Task PatchCaseData(ClaimsPrincipal user, Guid caseId, JsonNode patch);

    /// <summary>
    /// Performs a Partial Upgrade of the case data and does a json instance-schema validation of
    /// the case type's schema (<see cref="CaseType.DataSchema"/>).
    /// <remarks><see cref="JsonPatch"/></remarks>
    /// </summary>
    /// <param name="user">The current user</param>
    /// <param name="caseId">The case id</param>
    /// <param name="operations">https://indice.visualstudio.com/Platform/_wiki/wikis/Platform.wiki/1613/Patch-Case-Data-API</param>
    /// <returns>A <see cref="Task"/> representing an asynchronous operation</returns>
    Task PatchCaseData(ClaimsPrincipal user, Guid caseId, JsonPatch operations);

    /// <summary>Submit the case. Case must be in <strong>Draft</strong> mode.</summary>
    /// <param name="user">The user that initiated the submission.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>A <see cref="Task"/> representing an asynchronous operation</returns>
    Task Submit(ClaimsPrincipal user, Guid caseId);

    /// <summary>Get a list of cases as defined by <see cref="GetCasesListFilter"/> and the role of the user.</summary>
    /// <param name="user">The user that initiated the request</param>
    /// <param name="options">The case list filters.</param>
    /// <returns>A <see cref="Task{ResultSet}"/> representing an asynchronous operation</returns>
    Task<ResultSet<CasePartial>> GetCases(ClaimsPrincipal user, ListOptions<GetCasesListFilter> options);

    /// <summary>Get a case for a user by its Id</summary>
    /// <param name="user">The user that creates the request.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="includeAttachmentData">Include the attachment data with the response.</param>
    /// <returns>A <see cref="Task{Case}"/> representing the asynchronous operation</returns>
    Task<Case> GetCaseById(ClaimsPrincipal user, Guid caseId, bool? includeAttachmentData = null);

    /// <summary>Performs a physical delete for a draft case.</summary>
    /// <param name="user">The user that created the case.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>A <see cref="Task"/> representing an asynchronous operation</returns>
    Task DeleteDraft(ClaimsPrincipal user, Guid caseId);

    /// <summary>Get an attachment for a user by its Id</summary>
    /// <param name="user">The user that creates the request.</param>
    /// <param name="attachmentId">The Id of the attachment.</param>
    /// <returns>An object that points to the attachement file created</returns>
    Task<CaseAttachment> GetAttachmentById(ClaimsPrincipal user, Guid attachmentId);

    /// <summary>Assign a case to the actor that initiated this method.</summary>
    /// <param name="user">The user that initiated the call, and will be self-assigned to the case.</param>
    /// <param name="caseId">The Id of the case to be assigned.</param>
    /// <returns>The <see cref="AuditMeta"/> that holds audit information</returns>
    Task<AuditMeta> AssignCase(AuditMeta user, Guid caseId);

    /// <summary>Clears the assignment for a case.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>A <see cref="Task"/> representing an asynchronous operation</returns>
    Task RemoveAssignment(Guid caseId);

    /// <summary>Get the timeline entries for a case.</summary>
    /// <param name="user">The user that creates the request.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>A list of <seealso cref="TimelineEntry"/></returns>
    Task<List<TimelineEntry>> GetTimeline(ClaimsPrincipal user, Guid caseId);

    /// <summary>
    /// Gets the cases that are related to the given id.
    /// Set a value to the case's metadata with the key ExternalCorrelationKey to correlate cases.
    /// </summary>
    /// <param name="user">The user that creates the request.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>A list of <seealso cref="CasePartial"/></returns>
    Task<List<CasePartial>> GetRelatedCases(ClaimsPrincipal user, Guid caseId);

    /// <summary>Get a list of attachments by CaseId</summary>
    /// <param name="caseId"></param>
    /// <returns>A list of <seealso cref="CaseAttachment"/></returns>
    Task<ResultSet<CaseAttachment>> GetAttachments(Guid caseId);

    /// <summary>Get single Case Attachment data</summary>
    /// <param name="caseId">The case id</param>
    /// <param name="attachmentId">The attachment id</param>
    /// <returns>The <seealso cref="CaseAttachment"/> or null</returns>
    Task<CaseAttachment?> GetAttachment(Guid caseId, Guid attachmentId);

    /// <summary>
    /// Gets a single Case Attachment by field name
    /// </summary>
    /// <param name="user">The current user</param>
    /// <param name="caseId">The case id</param>
    /// <param name="fieldName">The attachment filename</param>
    /// <returns>The <seealso cref="CaseAttachment"/> or null</returns>
    Task<CaseAttachment?> GetAttachmentByField(ClaimsPrincipal user, Guid caseId, string fieldName);

    /// <summary>
    /// Adds or edits metadata for a case.
    /// </summary>
    /// <param name="caseId">The case id</param>
    /// <param name="User">The user that creates the request.</param>
    /// <param name="metadata">The metadata to add or edit.</param>
    /// <returns>True in case of success</returns>
    Task<bool> PatchCaseMetadata(Guid caseId, ClaimsPrincipal User, Dictionary<string, string> metadata);
}

/// <summary>
/// Extension methods on the <see cref="IAdminCaseService"/>
/// </summary>
public static class AdminCaseServiceExtensions
{
    /// <summary>Patches Case Data using a Json Serializable object.</summary>
    public static async Task PatchCaseData<TValue>(this IAdminCaseService adminCaseService, ClaimsPrincipal user, Guid caseId, TValue patch) where TValue : notnull
        => await adminCaseService.PatchCaseData(user, caseId, patch.ToJsonNode() ?? throw new ArgumentNullException(nameof(patch), "Patch Operation cannot be null."));
}