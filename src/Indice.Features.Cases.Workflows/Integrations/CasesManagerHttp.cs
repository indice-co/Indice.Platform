using Indice.Features.Cases.Workflows.Models;

namespace Indice.Features.Cases.Workflows.Integrations;

/// <inheritdoc />
internal class CasesManagerHttp(CasesManagerHttpClient client) : ICasesManager
{
    private readonly CasesManagerHttpClient _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc />
    public async Task<Case> GetCaseById(Guid caseId, bool includeAttachments = false, bool fetchPublicData = false) {
        return await _client.GetByIdAsync(caseId, fetchPublicData, includeAttachments);
    }

    /// <inheritdoc />
    public async Task Send(Guid caseId, Actor actor, Message message) {
        if (actor == null) {
            throw new ArgumentNullException(nameof(actor));
        }
        FileParameter? fileParameter = null;
        if (message.File is not null) {
            fileParameter = new FileParameter(new MemoryStream(message.File.Data), message.File.Name, message.File.ContentType);
        }

        await _client.SendMessageAsync(
            caseId: caseId,
            replyToCommentId: message.ReplyToCommentId,
            checkpointTypeName: message.CheckpointTypeName,
            privateComment: message.PrivateComment,
            comment: message.Comment,
            fileStreamAccessor: new FuncOfStream(),
            fileName: message.File?.Name,
            data: message.Data,
            file: fileParameter,
            actorId: actor.Id,
            actorReference: actor.Reference,
            actorGroupId: actor.GroupId,
            actorName: actor.Name,
            actorTin: actor.Tin,
            actorEmail: actor.Email,
            actorCurrentCulture: actor.CurrentCulture
        );
    }

    /// <inheritdoc />
    public async Task PatchData(Guid caseId, object data, bool patchPublicData = false) {
        await _client.PatchDataAsync(caseId, new PatchDataRequest {
            CaseData = data,
            PatchPublicData = patchPublicData
        });
    }

    /// <inheritdoc />
    public async Task JsonPatchData(Guid caseId, List<PatchJsonPathRequest> patches, bool patchPublicData = false) {
        await _client.JsonPatchDataAsync(caseId, new JsonPatchDataRequest {
            PatchPublicData = patchPublicData,
            JsonPatch = patches
        });
    }

    public async Task AttachFile(Guid caseId, Actor actor, File file, string? comment, string caseDataRootKey) {
        ArgumentNullException.ThrowIfNull(actor);
        var fileParameter = new FileParameter(new MemoryStream(file.Data), file.Name, file.ContentType);
        await _client.AttachFileAsync(caseId, fileParameter, comment, caseDataRootKey, actor.Id, actor.Reference, actor.GroupId, actor.Name, actor.Tin, actor.Email, actor.CurrentCulture);
    }

    public async Task<CaseAttachment?> GetAttachment(Guid caseId, Guid attachmentId) {
        try {
            return await _client.GetAttachmentAsync(caseId, attachmentId);
        } catch (ApiException ex) when (ex.StatusCode == (int)System.Net.HttpStatusCode.NotFound) {
            return null!;
        }
    }

    public async Task<CaseAttachmentResultSet> GetAttachments(Guid caseId) {
        return await _client.GetAttachmentsAsync(caseId);
    }

    /// <inheritdoc />
    public async Task<bool> PatchMetadata(Guid caseId, IDictionary<string, string> metadata) {
        return await _client.PatchMetadataAsync(caseId, metadata);
    }

    /// <inheritdoc />
    public async Task AddApproval(Guid caseId, Approval action, string? reason, Actor actor) {
        await _client.AddApprovalAsync(caseId, new WorkflowAddApprovalRequest {
            Action = Approval.Approve,
            Reason = null,
            WorkflowActor = actor.ToCasesActor()
        });
    }

    /// <inheritdoc />
    public async Task AddApprovalWithComment(Guid caseId, Approval action, string? reason, bool isPrivate, Actor actor) {
        await _client.AddApprovalWithCommentAsync(caseId, new WorkflowAddApprovalWithCommentRequest {
            Action = action,
            Reason = reason,
            PrivateComment = isPrivate,
            WorkflowActor = actor.ToCasesActor()
        });
    }

    /// <inheritdoc />
    public async Task<AuditMeta> AssignToActor(UserActor actor, Guid caseId) {
        return await _client.AssignAsync(caseId, actor);
    }

    /// <inheritdoc />
    public async Task BlockPreviousApprover(Guid caseId, Actor actor) {
        await _client.BlockPreviousApproverAsync(caseId, actor.ToCasesActor());
    }

    /// <inheritdoc />
    public Task<CaseApproval?> GetLastApproval(Guid caseId) {
        try {
            return _client.GetLastApprovalAsync(caseId);
        } catch (ApiException ex) when (ex.StatusCode == (int)System.Net.HttpStatusCode.NotFound) {
            return null!;
        }
    }

    /// <inheritdoc />
    public async Task RemoveAssignment(Guid caseId) {
        await _client.RemoveAssignmentAsync(caseId);
    }

    /// <inheritdoc />
    public async Task RollbackApproval(Guid caseId) {
        await _client.RollbackApprovalAsync(caseId);
    }

    /// <inheritdoc />
    public async Task<ICollection<NotificationSubscription>> GetNotificationSubscriptions(int? page, int? size, string sort, string search,
        IEnumerable<string> email, IEnumerable<string> groupId, IEnumerable<Guid> caseTypeIds) =>
        await _client.GetNotificationSubscriptionsAsync(page, size, sort, search, email, groupId, caseTypeIds);
}