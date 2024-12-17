using System.Security.Claims;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Events;
using Indice.Events;

namespace Indice.Features.Cases.Core.Services.CaseMessageService;

internal abstract class BaseCaseMessageService
{
    protected CasesDbContext DbContext { get; }
    private readonly IPlatformEventService _platformEventService;
    private readonly ISchemaValidator _schemaValidator;

    protected BaseCaseMessageService(
        CasesDbContext dbContext,
        IPlatformEventService platformEventService,
        ISchemaValidator schemaValidator) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _platformEventService = platformEventService ?? throw new ArgumentNullException(nameof(platformEventService));
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
    }

    protected async Task<Guid?> SendInternal(DbCase @case, Message message, ClaimsPrincipal user) {
        Guid? attachmentId = null;
        var caseId = @case.Id;
        ArgumentNullException.ThrowIfNull(message);
        if (message.FileStreamAccessor == null && message.CheckpointTypeName == null && message.Comment == null && message.Data == null) {
            return attachmentId;
        }

        var caseType = await DbContext.CaseTypes.FindAsync(@case.CaseTypeId);
        if (caseType == null) throw new ArgumentNullException(nameof(caseType));

        await _platformEventService.Publish(new CaseMessageCreatedEvent(caseId, message));

        var newCheckpointType = await DbContext.CheckpointTypes
            .AsQueryable()
            .SingleOrDefaultAsync(x => x.Code == message.CheckpointTypeName && x.CaseTypeId == caseType.Id);
        if (message.ReplyToCommentId.HasValue) {
            var exists = await DbContext.Comments.AsQueryable().AnyAsync(x => x.CaseId == caseId && x.Id == message.ReplyToCommentId.Value);
            if (!exists) {
                throw new Exception("Invalid reply to comment id. Not found on the current case.");
            }
        }

        if (message.FileStreamAccessor == null && message.CheckpointTypeName == null && message.Data == null) {
            await AddComment(user, caseId, message.Comment, message.ReplyToCommentId, message.PrivateComment);
        } else if (message.FileStreamAccessor != null && message.CheckpointTypeName == null) {
            var attachment = await AddAttachment(user, @case, message.Comment, message.FileName!, message.FileStreamAccessor!);
            attachmentId = attachment.Id;
        } else if (message.FileStreamAccessor == null && message.CheckpointTypeName != null) {
            //TODO: if message has both Data and Checkpoint name, then Data does not save (check 64 line)
            await AddCheckpoint(user, @case, newCheckpointType!);
            if (!string.IsNullOrWhiteSpace(message.Comment)) {
                message.PrivateComment ??= newCheckpointType!.Private;
                await AddComment(user, caseId, message.Comment, message.ReplyToCommentId, message.PrivateComment);
            }
        } else if (message.Data != null) {
            await AddCaseData(user, @case, message.Data);
            if (!string.IsNullOrWhiteSpace(message.Comment)) {
                await AddComment(user, caseId, message.Comment, message.ReplyToCommentId, message.PrivateComment);
            }
        } else {
            await AddCheckpoint(user, @case, newCheckpointType!);
            var attachment = await AddAttachment(user, @case, string.Empty, message.FileName!, message.FileStreamAccessor!);
            attachmentId = attachment.Id;
        }
        if (!string.IsNullOrEmpty(message.CheckpointTypeName)) {
            if (!message.PrivateComment.HasValue) {
                message.PrivateComment = newCheckpointType!.Private;
            }
            var suffix = $"Setting case to '{newCheckpointType!.Code}'";
            message.Comment = string.IsNullOrEmpty(message.Comment) ? suffix : $"{message.Comment}. {suffix}";
        }

        await DbContext.SaveChangesAsync();
        await _platformEventService.Publish(new CaseMessageSentEvent(caseId, message));
        return attachmentId;
    }

    protected Task SendInternal(DbCase @case, ClaimsPrincipal user, Exception exception, string? message) {
        var comment = string.IsNullOrEmpty(message)
            ? $"Faulted with message: {exception.Message}"
            : $"Faulted with message: {message} and exception message: {exception.Message}";
        return SendInternal(@case, new Message {
            Comment = comment,
            PrivateComment = true
        }, user);
    }

    private async Task AddComment(ClaimsPrincipal user, Guid caseId, string? text, Guid? replyToCommentId, bool? @private) {
        var newComment = new DbComment {
            IsCustomer = false, // todo decide if customer, from claims
            Private = @private ?? true,
            CaseId = caseId,
            Text = text,
            ReplyToCommentId = replyToCommentId,
            CreatedBy = AuditMeta.Create(user)
        };

        await DbContext.Comments.AddAsync(newComment);
    }

    private async Task<CasesAttachmentLink> AddAttachment(ClaimsPrincipal user, DbCase @case, string? comment, string fileName, Func<Stream> fileStreamAccessor) {
        var attachment = new DbAttachment(@case.Id);
        attachment.PopulateFrom(fileName, fileStreamAccessor, saveData: true);
        DbContext.Attachments.Add(attachment);
        @case.Attachments.Add(attachment);
        var link = new CasesAttachmentLink {
            Id = attachment.Id,
            FileGuid = attachment.Guid,
            Size = attachment.ContentLength,
            ContentType = attachment.ContentType,
            Label = attachment.Name,
            //PermaLink = $"/api/requests/{requestId}/files/{(Base64Id)attachment.Guid}"
        };
        var commentEntity = new DbComment {
            CreatedBy = AuditMeta.Create(user),
            CaseId = @case.Id,
            Private = true,
            AttachmentId = attachment.Id,
            Text = comment ?? $"{link.Label} {link.SizeText}"
        };
        await DbContext.Comments.AddAsync(commentEntity);
        return link;
    }

    private async Task<DbCheckpoint> AddCheckpoint(ClaimsPrincipal user, DbCase @case, DbCheckpointType checkpointType) {
        ArgumentNullException.ThrowIfNull(checkpointType);
        var checkpoint = await DbContext.Checkpoints
            .Include(p => p.CheckpointType)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == @case.CheckpointId);

        // If the new checkpoint is the same as the last attempt, only add the comment. 
        if (checkpoint != null && checkpoint.CheckpointType.Code == checkpointType.Code) {
            return checkpoint;
        }

        // Else continue to change the checkpoint.
        if (checkpoint != null) {
            checkpoint.CompletedDate = DateTimeOffset.UtcNow;
        }

        var nextCheckpoint = new DbCheckpoint {
            CaseId = @case.Id,
            CheckpointTypeId = checkpointType.Id,
            CreatedBy = AuditMeta.Create(user)
        };

        if (!checkpointType.Private) {
            @case.PublicCheckpointId = nextCheckpoint.Id;
        }

        if (checkpointType.Status == CaseStatus.Completed && @case.CompletedBy is null) {
            @case.CompletedBy = AuditMeta.Create(user);
            // fast-forward public data Id
            @case.PublicDataId = @case.DataId;
            // force remove assignment (if any)
            @case.AssignedTo = null;
        }

        @case.CheckpointId = nextCheckpoint.Id;
        @case.Checkpoints.Add(nextCheckpoint);
        await DbContext.Checkpoints.AddAsync(nextCheckpoint);
        return nextCheckpoint;
    }

    private async Task AddCaseData(ClaimsPrincipal user, DbCase @case, dynamic data) {
        if (data == null) throw new ArgumentNullException(nameof(data));

        // Validate data against case type json schema, only when schema is present
        if (@case.CaseType.DataSchema is not null && !_schemaValidator.IsValid(@case.CaseType.DataSchema, (object)data)) {
            throw new Exception("Data validation error.");
        }

        var newDataVersion = new DbCaseData {
            Case = @case,
            CreatedBy = AuditMeta.Create(user),
            Data = data
        };

        @case.DataId = newDataVersion.Id;
        
        // If case is mine, my changes are also publicly visible
        if (@case.CreatedBy.Id == user.FindSubjectIdOrClientId()) {
            @case.PublicDataId = newDataVersion.Id;
        }

        // Update checkpoint data
        await DbContext.CaseData.AddAsync(newDataVersion);
    }
}