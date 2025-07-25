using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>WorkflowHandler</summary>
internal static class IntegrationHandlers
{
    /// <summary> Gets the Admin case for the specified caseId.</summary>
    public static async Task<Results<Ok<Case>, NotFound>> GetById(
        Guid caseId,
        bool fetchPublicData,
        IOptions<CasesOptions> casesOptions,
        IAdminCaseService adminCaseService,
        bool includeAttachments = false
    ) => TypedResults.Ok(await adminCaseService.GetCaseById(caseId, fetchPublicData, includeAttachments));

    /// <summary>Gets the Last Approval</summary>
    public static async Task<Results<Ok<CaseApproval>, NotFound>> GetLastApproval(Guid caseId, ICaseApprovalService caseApprovalService) {
        var lastApproval = await caseApprovalService.GetLastApproval(caseId);
        return lastApproval is not null ? TypedResults.Ok(lastApproval) : TypedResults.NotFound();
    }

    /// <summary>Sends a message as Admin for a case.</summary>
    public static async Task SendMessage(
        Guid caseId,
        MessageRequest request,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions,
        IAdminCaseMessageService adminCaseMessageService
    ) => await adminCaseMessageService.Send(caseId, currentUser.UserToActor(casesOptions.Value), request.Message, request.Actor.ToAuditMeta());

    /// <summary>Assign a Case to an Actor.</summary>
    public static async Task<Ok<AuditMeta>> Assign(Guid caseId, UserActor actor, IAdminCaseService adminCaseService) {
        var assignedTo = await adminCaseService.AssignCase(actor.ToAuditMeta(), caseId);
        return TypedResults.Ok(assignedTo);
    }

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
        IOptions<CasesOptions> casesOptions,
        IAdminCaseMessageService caseMessageService,
        CaseSharedResourceService caseSharedResourceService,
        ICaseApprovalService caseApprovalService
    ) {
        var createdBy = request.WorkflowActor.ToAuditMeta();
        await caseMessageService.Send(caseId, currentUser.UserToActor(casesOptions.Value), new Message {
            Comment = caseSharedResourceService.GetLocalizedHtmlString(request.Reason ?? string.Empty).Value,
            PrivateComment = request.PrivateComment
        }, createdBy);

        await caseApprovalService.AddApproval(caseId, null, request.Action, request.Reason, createdBy);
    }

    /// <summary>Remove the assignment of a Case.</summary>
    public static async Task RemoveAssignment(Guid caseId, IAdminCaseService adminCaseService)
        => await adminCaseService.RemoveAssignment(caseId);

    /// <summary>Remove assignment for a Case and Send a message for the UI.</summary>
    public static async Task<Ok> BlockPreviousApprover(
        Guid caseId,
        UserActor actor,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions,
        CasesMessageDescriber casesMessageDescriber,
        IAdminCaseMessageService caseMessageService,
        IAdminCaseService adminCaseService
    ) {
        await caseMessageService.Send(caseId, currentUser.UserToActor(casesOptions.Value), new Message {
            Comment = casesMessageDescriber.BlockPreviousApproverCommentWithCulture(actor.CurrentCulture ?? "el-GR"),
            PrivateComment = true
        }, actor.ToAuditMeta());

        await adminCaseService.RemoveAssignment(caseId);

        return TypedResults.Ok();
    }

    /// <summary>Rollback an approval</summary>
    /// <param name="caseId"></param>
    /// <param name="caseApprovalService"></param>
    public static async Task RollbackApproval(Guid caseId, ICaseApprovalService caseApprovalService)
        => await caseApprovalService.RollbackApproval(caseId);

    /// <summary>Sync private data to public</summary>
    /// <param name="caseId"></param>
    /// <param name="adminCaseService"></param>
    public static async Task PublishPrivateData(Guid caseId, IAdminCaseService adminCaseService)
        => await adminCaseService.PublishData(caseId);

    /// <summary>Patch Case Data.</summary>
    public static async Task PatchData(
        Guid caseId,
        PatchDataRequest request,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions,
        IAdminCaseService adminCaseService
    ) => await adminCaseService.PatchCaseData(currentUser.UserToActor(casesOptions.Value), caseId, request.CaseData, request.PatchPublicData);

    /// <summary>Patch Case Data.</summary>
    public static async Task JsonPatchData(
        Guid caseId,
        JsonPatchDataRequest request,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions,
        IAdminCaseService adminCaseService
    ) => await adminCaseService.PatchCaseData(currentUser.UserToActor(casesOptions.Value), caseId, request.JsonPatch, request.PatchPublicData);

    /// <summary>Patch Case Metadata</summary>
    public static async Task<bool> PatchMetadata(
        Guid caseId,
        Dictionary<string, string> metadata,
        IAdminCaseService adminCaseService
    ) => await adminCaseService.PatchCaseMetadata(caseId, metadata);

    public static async Task<Results<Ok, ValidationProblem>> AttachFile(
        Guid caseId,
        AttachFileRequest request,
        IAdminCaseService adminCaseService,
        IAdminCaseMessageService adminCaseMessageService
    ) {
        var file = request.File;
        
        if (!(file?.Length > 0)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(file), "File is empty ."));
        }
        
        if (string.IsNullOrWhiteSpace(request.DataRootKey)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(request.DataRootKey), "Data root key not provided."));
        }
        
        var attachmentId = await adminCaseMessageService.Send(caseId, request.Actor, new Message {
            FileName = file.FileName,
            FileStreamAccessor = file.OpenReadStream,
            Comment = request.Comment
        });
        
        await adminCaseService.PatchCaseData(request.Actor, caseId, new JsonObject{ [request.DataRootKey] = attachmentId.ToString() }, false);

        return TypedResults.Ok();
    }

    public static async Task<Results<Ok<CaseAttachment>, NotFound>> GetAttachment(Guid caseId, Guid attachmentId, IAdminCaseService adminCaseService) {
        var attachment = await adminCaseService.GetAttachment(caseId, attachmentId);
        return attachment is null ? TypedResults.NotFound() : TypedResults.Ok(attachment);
    }
    
    /// <summary>Gets all attachments of a case by id.</summary>
    public static async Task<Ok<ResultSet<CaseAttachment>>> GetAttachments(Guid caseId, IAdminCaseService adminCaseService) =>
        TypedResults.Ok(await adminCaseService.GetAttachments(caseId));

    public class AttachFileRequest
    {
        /// <summary>File data</summary>
        [Required]
        public IFormFile? File { get; set; }
        
        /// <summary>The comment with which to notify the user for the file upload.</summary>
        public string? Comment { get; set; }
        
        /// <summary>The root element of the Case Data that will be added/replaced with the attachmentId.</summary>
        public string? DataRootKey { get; set; }
        
        /// <summary>The Id of the user.</summary>
        public required string ActorId { get; init; }

        /// <summary>Can be the customer id or something related to an external system correlation id.</summary>
        public string? ActorReference { get; init; }

        /// <summary>The group id claim value.</summary> 
        public string? ActorGroupId { get; init; }

        /// <summary>The name of the user.</summary>
        public string? ActorName { get; init; }

        /// <summary>The tin of the user.</summary>
        public string? ActorTin { get; init; }

        /// <summary>The email of the user.</summary>
        public string? ActorEmail { get; init; }

        /// <summary>The current culture of the user.</summary>
        public string? ActorCurrentCulture { get; init; }
        
        /// <summary>Actor</summary>
        internal UserActor Actor => new UserActor { Id = ActorId, Reference = ActorReference, GroupId = ActorGroupId, Name = ActorName, Tin = ActorTin, Email = ActorEmail, CurrentCulture = ActorCurrentCulture, IsSystemClient = true, IsAdmin = false };

        /// <summary>Bind method</summary>
        public static async ValueTask<AttachFileRequest> BindAsync(HttpContext context, ParameterInfo parameter) {
            var form = await context.Request.ReadFormAsync();
            var file = form.Files[nameof(File)];
            
            return new AttachFileRequest {
                File = file,
                Comment = form[nameof(Comment)],
                DataRootKey = form[nameof(DataRootKey)],
                ActorId = form[nameof(ActorId)]!,
                ActorReference = form[nameof(ActorReference)],
                ActorGroupId = form[nameof(ActorGroupId)],
                ActorName = form[nameof(ActorName)],
                ActorTin = form[nameof(ActorTin)],
                ActorEmail = form[nameof(ActorEmail)],
                ActorCurrentCulture = form[nameof(ActorCurrentCulture)],
            };
        }
    }


    /// <summary>PatchDataRequest</summary>
    public class PatchDataRequest
    {
        public JsonNode CaseData { get; set; }
        public bool PatchPublicData { get; set; }
    }

    /// <summary>JsonPatchDataRequest</summary>
    public class JsonPatchDataRequest
    {
        public List<PatchJsonPathRequest> JsonPatch { get; set; } = [];
        public bool PatchPublicData { get; set; }
    }

    /// <summary>WorkflowAddApprovalRequest</summary>
    public class WorkflowAddApprovalRequest
    {
        /// <summary>Approval Action.</summary>
        public Approval Action { get; set; }

        /// <summary>Approval Reason.</summary>
        public string? Reason { get; set; }

        /// <summary>Actor responsible for this action.</summary>
        public UserActor WorkflowActor { get; set; } = null!;
    }

    /// <summary>WorkflowAddApprovalWithCommentRequest</summary>
    public class WorkflowAddApprovalWithCommentRequest
    {
        /// <summary>Approval Action.</summary>
        public Approval Action { get; set; }

        /// <summary>Approval Reason.</summary>
        public string? Reason { get; set; }

        /// <summary>Actor responsible for this action.</summary>
        public UserActor WorkflowActor { get; set; } = null!;

        /// <summary>Comment Private or not.</summary>
        public bool PrivateComment { get; set; }
    }

    public class MessageRequest : Message
    {
        /// <summary>File data</summary>
        public IFormFile? File { get; set; }

        /// <summary>The Id of the user.</summary>
        public required string ActorId { get; init; }

        /// <summary>Can be the customer id or something related to an external system correlation id.</summary>
        public string? ActorReference { get; init; }

        /// <summary>The group id claim value.</summary> 
        public string? ActorGroupId { get; init; }

        /// <summary>The name of the user.</summary>
        public string? ActorName { get; init; }

        /// <summary>The tin of the user.</summary>
        public string? ActorTin { get; init; }

        /// <summary>The email of the user.</summary>
        public string? ActorEmail { get; init; }

        /// <summary>The current culture of the user.</summary>
        public string? ActorCurrentCulture { get; init; }

        internal Message Message => new Message { ReplyToCommentId = ReplyToCommentId, CheckpointTypeName = CheckpointTypeName, PrivateComment = PrivateComment, Comment = Comment, Data = Data, FileStreamAccessor = FileStreamAccessor, FileName = FileName };

        /// <summary>Actor</summary>
        internal UserActor Actor => new UserActor { Id = ActorId, Reference = ActorReference, GroupId = ActorGroupId, Name = ActorName, Tin = ActorTin, Email = ActorEmail, CurrentCulture = ActorCurrentCulture, IsSystemClient = true, IsAdmin = false };

        /// <summary>Bind method</summary>
        public static async ValueTask<MessageRequest> BindAsync(HttpContext context, ParameterInfo parameter) {
            var form = await context.Request.ReadFormAsync();
            var file = form.Files[nameof(File)];
            
            return new MessageRequest {
                ReplyToCommentId = Guid.TryParse(form[nameof(ReplyToCommentId)], CultureInfo.InvariantCulture, out var replyToCommentId) ? replyToCommentId : null,
                CheckpointTypeName = form[nameof(CheckpointTypeName)],
                PrivateComment = bool.TryParse(form[nameof(PrivateComment)], out var privateComment) ? privateComment : null,
                Comment = form[nameof(Comment)],
                Data = StringValues.IsNullOrEmpty(form[nameof(Data)]) ? null : JsonNode.Parse(form[nameof(Data)].ToString()),
                FileName = file?.FileName,
                FileStreamAccessor = file is null ? null : file.OpenReadStream,
                ActorId = form[nameof(ActorId)]!,
                ActorReference = form[nameof(ActorReference)],
                ActorGroupId = form[nameof(ActorGroupId)],
                ActorName = form[nameof(ActorName)],
                ActorTin = form[nameof(ActorTin)],
                ActorEmail = form[nameof(ActorEmail)],
                ActorCurrentCulture = form[nameof(ActorCurrentCulture)],
            };
        }
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

**JsonIgnoreCondition.WhenWritingNull must NOT be set in the nswag client serializer if you want to remove a property**
https://indice.visualstudio.com/Platform/_wiki/wikis/Platform.wiki/1613/Patch-Case-Data-API"">Documentation
For handling nested arrays, moving elements, checking if data exists at specified locations use JsonPatch API
```csharp
_casesApiClient.PatchAdminCaseDataAsync(caseId, null, new { t1 = ""test"", t2 = (object)null! }
```
- **If the path is found ""add"" works as add or replace.**
- **This will NOT create non existing paths, be sure to specify the full object as value on a JsonPointer that exists.**
";

    public const string JsonPatchDataDescription = @"
Update the Case Data for the specific caseId according to https://datatracker.ietf.org/doc/html/rfc6902#appendix-A
Example Usage:
```csharp
_casesApiClient.JsonPatchAdminCaseDataAsync(caseId, null, new PatchJsonPathRequest[] {
   Operations = new PatchOperation[] {
     new() { Op = OperationType.Add, Path = ""/t1"", Value = ""test"" },
     new() { Op = OperationType.Remove, Path = ""/t2"" }
}
- **If the path is found ""add"" works as add or replace.**
- **This will NOT create non existing paths, be sure to specify the full object as value on a JsonPointer that exists.**
```
";

    public const string PatchMetadataDescription = @"
Patches the Metadata of the Case from a Dictionary<string, string>
Note that this runs as a systemic user and does not perform ANY authorization as opposed to the front facing endpoint.
This should return a boolean response indicating if the Metadata was actually updated.
";

    #endregion
}