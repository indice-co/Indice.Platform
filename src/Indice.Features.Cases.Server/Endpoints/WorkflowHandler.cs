using System.Security.Claims;
using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>
/// These are internal http endpoints so they will not necessarily follow http semantics on responses.
/// Convenience responses - generally OK - should be used to follow method-like semantics.
/// </summary>
internal static class WorkflowHandler
{
    /// <summary> Gets the Admin case for the specified caseId.</summary>
    public static async Task<Results<Ok<Case>, NotFound>> GetById(
        Guid caseId,
        ClaimsPrincipal currentUser, // todo: add workflow actor
        IAdminCaseService adminCaseService,
        bool includeAttachments = false
    ) => TypedResults.Ok(await adminCaseService.GetCaseById(currentUser, caseId, includeAttachments));

    /// <summary>Sends a message as Admin for a case.</summary>
    public static async Task SendMessage(
        Guid caseId,
        WorkflowSendMessageRequest request,
        ClaimsPrincipal currentUser,
        IAdminCaseMessageService adminCaseMessageService
    ) => await adminCaseMessageService.Send(caseId, currentUser, request.Message, request.WorkflowActor.ToAuditMeta());

    /// <summary>Patch Case Data.</summary>
    public static async Task PatchData(
        Guid caseId,
        JsonNode caseData,
        ClaimsPrincipal currentUser,
        IAdminCaseService adminCaseService
    ) => await adminCaseService.PatchCaseData(currentUser, caseId, caseData);

    /// <summary>Patch Case Metadata</summary>
    public static async Task<bool> PatchMetadata(
        Guid caseId,
        Dictionary<string, string> metadata,
        ClaimsPrincipal currentUser,
        IAdminCaseService adminCaseService
    ) => await adminCaseService.PatchCaseMetadata(caseId, currentUser, metadata);

    /// <summary>Rollback an approval</summary>
    /// <param name="caseId"></param>
    /// <param name="caseApprovalService"></param>
    public static async Task RollbackApproval(Guid caseId, ICaseApprovalService caseApprovalService) 
        => await caseApprovalService.RollbackApproval(caseId);

    /// <summary>Gets the Last Approval</summary>
    public static async Task<Ok<CaseApproval>> GetLastApproval(Guid caseId, ICaseApprovalService caseApprovalService) 
        => TypedResults.Ok(await caseApprovalService.GetLastApproval(caseId));

    /// <summary>Adds an approval to a case.</summary>
    public static async Task AddApproval(
        Guid caseId,
        WorkflowAddApprovalRequest request,
        IAdminCaseMessageService caseMessageService,
        ICaseApprovalService caseApprovalService
    ) => await caseApprovalService.AddApproval(caseId, null, request.Action, request.Reason, request.WorkflowActor.ToAuditMeta());

    /// <summary>Adds an approval with comment. To be used when adding an approval </summary>
    public static async Task AddApprovalWithComment(
        Guid caseId,
        WorkflowAddApprovalWithCommentRequest request,
        ClaimsPrincipal currentUser,
        IAdminCaseMessageService caseMessageService,
        CaseSharedResourceService caseSharedResourceService,
        ICaseApprovalService caseApprovalService
    ) {
        var createdBy = request.WorkflowActor.ToAuditMeta();
        await caseMessageService.Send(caseId, currentUser, new Message {
            Comment = caseSharedResourceService.GetLocalizedHtmlString(request.Reason ?? string.Empty).Value,
            PrivateComment = request.PrivateComment
        }, createdBy);

        await caseApprovalService.AddApproval(caseId, null, request.Action, request.Reason, createdBy);
    }
    
    /// <summary>Assign a Case to an Actor.</summary>
    public static async Task<Ok<AuditMeta>> Assign(Guid caseId, WorkflowActor actor, IAdminCaseService adminCaseService) {
        var assignedTo = await adminCaseService.AssignCase(actor.ToAuditMeta(), caseId);
        return TypedResults.Ok(assignedTo);
    }
    
    /// <summary>Remove the assignment of a Case.</summary>
    public static async Task RemoveAssignment(Guid caseId, IAdminCaseService adminCaseService) 
        => await adminCaseService.RemoveAssignment(caseId);

    /// <summary>Remove assignment for a Case and Send a message for the UI.</summary>
    public static async Task<Ok> BlockPreviousApprover(
        Guid caseId,
        WorkflowActor actor,
        ClaimsPrincipal currentUser,
        CasesMessageDescriber casesMessageDescriber,
        IAdminCaseMessageService caseMessageService,
        IAdminCaseService adminCaseService
    ) {
        await caseMessageService.Send(caseId, currentUser, new Message {
            Comment = casesMessageDescriber.BlockPreviousApproverCommentWithCulture("en-US"), // todo: pass culture in body
            PrivateComment = true
        }, actor.ToAuditMeta());
        
        await adminCaseService.RemoveAssignment(caseId);
        
        return TypedResults.Ok();
    }
    
    #region endpoint description

    public const string PatchDataDescription = @"
    Patches the Case.Data object with an object passed in the body.
    If Data is invalid due to schema validation failure, no change will happen and an error 500 will be returned
    Recursively merges two JsonNodes by ensuring that the structure of the `original` node is
    updated defensively preventing overwrites of incompatible JsonNode types.
    1. If the `toMerge` node contains multiple nested types, each one of them should exist as-is in the `original` node.
    2. If the `toMerge` node contains nested types that the `original` node only partially matches (subset),
      the remaining nested types are added to the corresponding location in the `original` node.
    3. When we encounter a JsonArray we add/replace from THIS POINT ON the nested element at the end of the array,
    and NOT replace any existing - nested or not - items.
    
    <b>JsonIgnoreCondition.WhenWritingNull must NOT be set in the nswag client serializer if you want to remove a property</b>
    https://indice.visualstudio.com/Platform/_wiki/wikis/Platform.wiki/1613/Patch-Case-Data-API"">Documentation
    For handling nested arrays, moving elements, checking if data exists at specified locations use JsonPatch API
    <code>
    _casesApiClient.PatchAdminCaseDataAsync(caseId, null, new { t1 = ""test"", t2 = (object)null! }
    </code>
    <b>If the path is found ""add"" works as add or replace.</b>
    <b>This will NOT create non existing paths, be sure to specify the full object as value on a JsonPointer that exists.</b>
    </param>
    ";

    public const string PatchMetadataDescription = @"
    Patches the Metadata of the Case from a Dictionary<string, string>
    Note that this runs as a systemic user and does not perform ANY authorization as opposed to the front facing endpoint.
    This should return a boolean response indicating if the Metadata was actually updated.
    ";

    #endregion
}