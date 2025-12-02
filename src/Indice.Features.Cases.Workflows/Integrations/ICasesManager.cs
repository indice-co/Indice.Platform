using Indice.Features.Cases.Workflows.Models;
using Indice.Types;

namespace Indice.Features.Cases.Workflows.Integrations;

/// <summary>Interface for interacting with Cases Integration Endpoints.</summary>
public interface ICasesManager
{
    /// <summary>
    /// <inheritdoc cref="CasesManagerHttpClient.GetByIdAsync(Guid, bool, bool?)"/>
    /// </summary>
    Task<Case> GetCaseById(Guid caseId, bool includeAttachments = false, bool fetchPublicData = false);

    /// <summary>
    /// Sends the message as the current Actor. This should be the default method for backwards compatibility.
    /// </summary>
    Task Send(Guid caseId, Actor actor, Message message);
    
    /// <summary>
    /// Patches the metadata of a case.
    /// </summary>
    /// <remarks>
    /// Patches the Metadata of the Case from a Dictionary&lt;string, string&gt;
    /// <br/>Note that this runs as a systemic user and does not perform ANY authorization as opposed to the front facing endpoint.
    /// <br/>This should return a boolean response indicating if the Metadata was actually updated.
    /// </remarks>
    Task<bool> PatchMetadata(Guid caseId, IDictionary<string, string> metadata);
    
    /// <summary>
    /// <inheritdoc cref="CasesManagerHttpClient.PatchDataAsync(Guid, PatchDataRequest)"/>
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="CasesManagerHttpClient.PatchDataAsync(Guid, PatchDataRequest)"/>
    /// </remarks>
    Task PatchData(Guid caseId, object data, bool patchPublicData = false);
    
    /// <summary>
    /// <inheritdoc cref="CasesManagerHttpClient.JsonPatchDataAsync(Guid, JsonPatchDataRequest)"/>
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="CasesManagerHttpClient.JsonPatchDataAsync(Guid, JsonPatchDataRequest)"/>
    /// </remarks>
    Task JsonPatchData(Guid caseId, List<PatchJsonPathRequest> patches, bool patchPublicData = false);

    /// <summary>Add attachment to a case and update the relative root element of the case data.</summary>
    Task AttachFile(Guid caseId, Actor actor, File file, string? comment, string caseDataRootKey);

    /// <summary>
    /// <inheritdoc cref="CasesManagerHttpClient.GetAttachmentAsync(Guid, Guid)"/>
    /// </summary>
    Task<CaseAttachment?> GetAttachment(Guid caseId, Guid attachmentId);
    
    /// <summary>
    /// <inheritdoc cref="CasesManagerHttpClient.GetAttachmentsAsync(Guid)"/>
    /// </summary>
    Task<CaseAttachmentResultSet> GetAttachments(Guid caseId);

    /// <summary>
    /// <inheritdoc cref="CasesManagerHttpClient.GetCaseTypeSubscribersAsync(string, int?, int?, string, string, IEnumerable{string}, IEnumerable{string}, IEnumerable{Guid})"/>
    /// </summary>
    Task<NotificationSubscriptionResultSet> GetCaseTypeSubscribers(string caseTypeCode, int? page, int? size, string sort, string search, string[]? email, string[]? groupId);

    /// <summary>
    /// <inheritdoc cref="CasesManagerHttpClient.AddApprovalAsync(Guid, WorkflowAddApprovalRequest)"/>
    /// </summary>
    internal Task AddApproval(Guid caseId, Approval action, string? reason, Actor actor);
    
    /// <summary>
    /// <inheritdoc cref="CasesManagerHttpClient.AddApprovalWithCommentAsync(Guid, WorkflowAddApprovalWithCommentRequest)"/>
    /// </summary>
    internal Task AddApprovalWithComment(Guid caseId, Approval action, string? reason, bool isPrivate, Actor actor);

    /// <summary>
    /// <inheritdoc cref="CasesManagerHttpClient.AssignAsync(Guid, UserActor)"/>
    /// </summary>
    internal Task<AuditMeta> AssignToActor(UserActor actor, Guid caseId);

    /// <summary>
    /// <inheritdoc cref="CasesManagerHttpClient.BlockPreviousApproverAsync(Guid, UserActor)"/>
    /// </summary>
    internal Task BlockPreviousApprover(Guid caseId, Actor actor);
    
    /// <summary>
    /// <inheritdoc cref="CasesManagerHttpClient.GetLastApprovalAsync(Guid)"/>
    /// </summary>
    internal Task<CaseApproval?> GetLastApproval(Guid caseId);
    
    /// <summary>
    /// <inheritdoc cref="CasesManagerHttpClient.RemoveAssignmentAsync(Guid)"/>
    /// </summary>
    internal Task RemoveAssignment(Guid caseId);
    
    /// <summary>
    /// <inheritdoc cref="CasesManagerHttpClient.RollbackApprovalAsync(Guid)"/>
    /// </summary>
    internal Task RollbackApproval(Guid caseId);
}

/// <summary>Workflow Message class for compatibility with Elsa</summary>
public class Message
{
    /// <summary>The Id of the Checkpoint the message is replying to.</summary>
    public Guid? ReplyToCommentId { get; set; }
        
    /// <summary>The name of the checkpoint the case must proceed.</summary>
    public string? CheckpointTypeName { get; set; }
    
    /// <summary>Indicates if the comment should be visible to the customer.</summary>
    public bool? PrivateComment { get; set; } = true;
    
    /// <summary>The comment to add to the checkpoint.</summary>
    public string? Comment { get; set; }
        
    /// <summary>The file name that is attached with the checkpoint.</summary>
    public File? File { get; set; }

    /// <summary>The data related with the message.</summary>
    public object? Data { get; set; }
}

/// <summary>File Parameter for uploads.</summary>
public class File
{
    /// <summary>Name of the file</summary>
    public string Name { get; set; } = null!;
    
    /// <summary>Content of the file.</summary>
    public byte[] Data { get; set; } = null!;
    
    /// <summary>Content type of the file.</summary>
    public string ContentType { get; set; } = null!;
    
    /// <summary>File Constructor.</summary>
    public File() {}
    
    /// <summary>File Constructor.</summary>
    public File(string name, byte[] data, string contentType) {
        Name = name;
        Data = data;
        ContentType = contentType;
    }

    /// <summary>File Constructor using a memory stream.</summary>
    public File(string name, MemoryStream stream, string contentType) : this(name, stream.ToArray(), contentType) { }

    /// <summary>File Constructor implicit convert to utf-8 encoding.</summary>
    public File(string name, string data, string contentType) : this(name, System.Text.Encoding.UTF8.GetBytes(data), contentType) { }
}