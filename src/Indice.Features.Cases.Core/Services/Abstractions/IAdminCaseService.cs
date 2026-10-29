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
    /// <param name="referenceNumber">The reference number the case might have.</param>
    /// <param name="channel">The channel the case was created from. Defaults to <strong>Managed</strong>.</param>
    /// <param name="relationKey">The key that correlates the case with other cases. Falls back to the <c>ExternalCorrelationKey</c> metadata entry and is stored there too.</param>
    /// <returns></returns>
    Task<CreateCaseResponse> CreateDraft(UserActor user, string caseTypeCode, string? groupId, ContactMeta? customer, Dictionary<string, string> metadata, string? referenceNumber, string? channel = null, string? relationKey = null);

    /// <summary>Update the case with the case data and does a json instance-schema validation of the case type's schema (<see cref="CaseType.DataSchema"/>).</summary>
    /// <param name="user">The user that will update the case.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="data">The case data (as defined by JSON Schema in CaseType).</param>
    /// <returns>A <see cref="Task"/> representing an asynchronous operation</returns>
    Task UpdateData(UserActor user, Guid caseId, JsonNode data);

    /// <summary>
    /// Performs a Partial Upgrade of the case data and does a json instance-schema validation of
    /// the case type's schema (<see cref="CaseType.DataSchema"/>).
    /// `Patch` can be any object e.g. JsonElement, JsonNode, JObject or JToken that can be (de)serialized as Json.
    /// https://indice.visualstudio.com/Platform/_wiki/wikis/Platform.wiki/1613/Patch-Case-Data-API
    /// </summary>
    /// <param name="user">The user that will update the case.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="patch">A JsonNode to merge with the existing case data.</param>
    /// <param name="patchPublicData">TODO</param>
    /// <returns>A <see cref="Task"/> representing an asynchronous operation</returns>
    Task PatchCaseData(UserActor user, Guid caseId, JsonNode patch, bool patchPublicData);

    /// <summary>
    /// Performs a Partial Upgrade of the case data and does a json instance-schema validation of
    /// the case type's schema (<see cref="CaseType.DataSchema"/>).
    /// <remarks><see cref="JsonPatch"/></remarks>
    /// </summary>
    /// <param name="user">The current user</param>
    /// <param name="caseId">The case id</param>
    /// <param name="operations">https://indice.visualstudio.com/Platform/_wiki/wikis/Platform.wiki/1613/Patch-Case-Data-API</param>
    /// <param name="patchPublicData">TODO</param>
    /// <returns>A <see cref="Task"/> representing an asynchronous operation</returns>
    Task PatchCaseData(UserActor user, Guid caseId, JsonPatch operations, bool patchPublicData);

    /// <summary>Submit the case. Case must be in <strong>Draft</strong> mode.</summary>
    /// <param name="user">The user that initiated the submission.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>A <see cref="Task"/> representing an asynchronous operation</returns>
    Task Submit(UserActor user, Guid caseId);

    /// <summary>Get a list of cases as defined by <see cref="GetCasesListFilter"/> and the role of the user.</summary>
    /// <param name="user">The user that initiated the request</param>
    /// <param name="options">The case list filters.</param>
    /// <returns>A <see cref="Task{ResultSet}"/> representing an asynchronous operation</returns>
    Task<ResultSet<CasePartial>> GetCases(UserActor user, ListOptions<GetCasesListFilter> options);

    /// <summary>Get a case for a user by its Id</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="fetchPublicData">Fetch the public data of case.</param>
    /// <param name="includeAttachmentData">Include the attachment data with the response.</param>
    /// <returns>A <see cref="Task{Case}"/> representing the asynchronous operation. The task result is <see langword="null"/> when no matching case is found.</returns>
    Task<Case?> GetCaseById(Guid caseId, bool fetchPublicData, bool? includeAttachmentData = null);

    /// <summary>Performs a physical delete for a draft case.</summary>
    /// <param name="user">The user that created the case.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>A <see cref="Task"/> representing an asynchronous operation</returns>
    Task DeleteDraft(UserActor user, Guid caseId);

    /// <summary>Get an attachment for a user by its Id</summary>
    /// <param name="attachmentId">The Id of the attachment.</param>
    /// <returns>An object that points to the attachement file created</returns>
    Task<CaseAttachment?> GetAttachmentById(Guid attachmentId);

    /// <summary>Assign a case to the actor that initiated this method.</summary>
    /// <param name="assignTo">The user that initiated the call, and will be self-assigned to the case.</param>
    /// <param name="caseId">The Id of the case to be assigned.</param>
    /// <returns>The <see cref="AuditMeta"/> that holds audit information</returns>
    Task<AuditMeta> AssignCase(AuditMeta assignTo, Guid caseId);

    /// <summary>Clears the assignment for a case.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>A <see cref="Task"/> representing an asynchronous operation</returns>
    Task RemoveAssignment(Guid caseId);

    /// <summary>Get the timeline entries for a case.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>A list of <seealso cref="TimelineEntry"/></returns>
    Task<List<TimelineEntry>> GetTimeline(Guid caseId);

    /// <summary>Get the versions of the data of a case, newest first.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="options">Paging and a search term matched against the name or email of the user that saved the version.</param>
    /// <returns>A page of <seealso cref="CaseDataVersion"/>, empty when the case does not exist.</returns>
    Task<ResultSet<CaseDataVersion>> GetDataVersions(Guid caseId, ListOptions options);

    /// <summary>
    /// Gets the cases that are related to the given id.
    /// Cases are related when they share the same <see cref="CasePartial.RelationKey"/>.
    /// It is set on case creation, or through the case's metadata with the key ExternalCorrelationKey.
    /// </summary>
    /// <param name="user">The user that creates the request.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>A list of <seealso cref="CasePartial"/></returns>
    Task<List<CasePartial>> GetRelatedCases(UserActor user, Guid caseId);

    /// <summary>
    /// Changes the relation key of a case, moving it to the cases with the new key. It leaves the cases of its old key,
    /// which stay related to each other; an empty key takes it out of any relation.
    /// </summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="relationKey">The new relation key.</param>
    /// <returns>False when the case does not exist.</returns>
    Task<bool> UpdateRelationKey(Guid caseId, string? relationKey);

    /// <summary>
    /// Relates cases to a case by giving them its relation key. A case that shared another key leaves that relation.
    /// </summary>
    /// <param name="caseId">The Id of the case. It must have a relation key.</param>
    /// <param name="relatedCaseIds">The Ids of the cases to relate.</param>
    /// <returns>False when the case does not exist.</returns>
    Task<bool> AddRelatedCases(Guid caseId, IEnumerable<Guid> relatedCaseIds);

    /// <summary>Removes a case from the relation of another case by clearing its relation key.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="relatedCaseId">The Id of the related case to remove.</param>
    /// <returns>False when either case does not exist or they are not related.</returns>
    Task<bool> RemoveRelatedCase(Guid caseId, Guid relatedCaseId);

    /// <summary>Changes the group of a case. The group takes part in the access rules, so this changes who can access the case.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="groupId">The new group id. Empty clears it.</param>
    /// <returns>False when the case does not exist.</returns>
    Task<bool> UpdateGroup(Guid caseId, string? groupId);

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
    Task<CaseAttachment?> GetAttachmentByField(UserActor user, Guid caseId, string fieldName);

    /// <summary>
    /// Adds or edits metadata for a case.
    /// </summary>
    /// <param name="caseId">The case id</param>
    /// <param name="metadata">The metadata to add or edit. An <c>ExternalCorrelationKey</c> entry also sets the case's <see cref="CasePartial.RelationKey"/>.</param>
    /// <returns>True in case of success</returns>
    Task<bool> PatchCaseMetadata(Guid caseId, Dictionary<string, string> metadata);

    /// <summary>
    /// Publish the latest version of Data.
    /// </summary>
    /// <param name="caseId">The case id</param>
    Task<bool> PublishData(Guid caseId);
    
}

/// <summary>
/// Extension methods on the <see cref="IAdminCaseService"/>
/// </summary>
public static class AdminCaseServiceExtensions
{
    /// <summary>Patches Case Data using a Json Serializable object.</summary>
    public static async Task PatchCaseData<TValue>(this IAdminCaseService adminCaseService, UserActor user, Guid caseId, TValue patch, bool patchPublicData) where TValue : notnull
        => await adminCaseService.PatchCaseData(user, caseId, patch.ToJsonNode() ?? throw new ArgumentNullException(nameof(patch), "Patch Operation cannot be null."), patchPublicData);
}