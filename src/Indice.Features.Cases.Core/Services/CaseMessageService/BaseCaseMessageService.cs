using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Events;
using Indice.Events;
using Indice.Types;

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

    protected async Task<Guid?> SendInternal(DbCase @case, Message message, AuditMeta createdBy) {
        Guid? attachmentId = null;
        var caseId = @case.Id;
        ArgumentNullException.ThrowIfNull(message);
        if (message.FileStreamAccessor == null && message.CheckpointTypeName == null && message.Comment == null && message.Data is null) {
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
                throw new BusinessException("Invalid reply to comment id. Not found on the current case.");
            }
        }

        if (message.FileStreamAccessor == null && message.CheckpointTypeName == null && message.Data is null) {
            await AddComment(createdBy, caseId, message.Comment, message.ReplyToCommentId, message.PrivateComment);
        } else if (message.FileStreamAccessor != null && message.CheckpointTypeName == null) {
            var attachment = await AddAttachment(createdBy, @case, message.Comment, message.FileName!, message.FileStreamAccessor!);
            attachmentId = attachment.Id;
        } else if (message.FileStreamAccessor == null && message.CheckpointTypeName != null) {
            //TODO: if message has both Data and Checkpoint name, then Data does not save (check 64 line)
            if (newCheckpointType == null) {
                throw new BusinessException($"Invalid checkpoint. Not checkpoint was found for the current case with name {message.CheckpointTypeName}.");
            }
            await AddCheckpoint(createdBy, @case, newCheckpointType!);
            if (!string.IsNullOrWhiteSpace(message.Comment)) {
                message.PrivateComment ??= newCheckpointType!.Private;
                await AddComment(createdBy, caseId, message.Comment, message.ReplyToCommentId, message.PrivateComment);
            }
        } else if (message.Data is not null) {
            await AddCaseData(createdBy, @case, message.Data);
            if (!string.IsNullOrWhiteSpace(message.Comment)) {
                await AddComment(createdBy, caseId, message.Comment, message.ReplyToCommentId, message.PrivateComment);
            }
        } else {
            await AddCheckpoint(createdBy, @case, newCheckpointType!);
            var attachment = await AddAttachment(createdBy, @case, string.Empty, message.FileName!, message.FileStreamAccessor!);
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

    private async Task AddComment(AuditMeta createdBy, Guid caseId, string? text, Guid? replyToCommentId, bool? @private) {
        var newComment = new DbComment {
            IsCustomer = false, // todo decide if customer, from claims
            Private = @private ?? true,
            CaseId = caseId,
            Text = text,
            ReplyToCommentId = replyToCommentId,
            CreatedBy = createdBy
        };

        await DbContext.Comments.AddAsync(newComment);
    }

    private async Task<CasesAttachmentLink> AddAttachment(AuditMeta createdBy, DbCase @case, string? comment, string fileName, Func<Stream> fileStreamAccessor) {
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
            CreatedBy = createdBy,
            CaseId = @case.Id,
            Private = true,
            AttachmentId = attachment.Id,
            Text = comment ?? $"{link.Label} {link.SizeText}"
        };
        await DbContext.Comments.AddAsync(commentEntity);
        return link;
    }

    private async Task AddCheckpoint(AuditMeta createdBy, DbCase @case, DbCheckpointType checkpointType) {
        ArgumentNullException.ThrowIfNull(checkpointType);
        var checkpoint = await DbContext.Checkpoints
            .Include(p => p.CheckpointType)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == @case.CheckpointId);

        // If the new checkpoint is the same as the last attempt, only add the comment. 
        if (checkpoint != null && checkpoint.CheckpointType.Code == checkpointType.Code) {
            return;
        }

        // Else continue to change the checkpoint.
        if (checkpoint != null) {
            checkpoint.CompletedDate = DateTimeOffset.UtcNow;
        }

        var nextCheckpoint = new DbCheckpoint {
            CaseId = @case.Id,
            CheckpointTypeId = checkpointType.Id,
            CreatedBy = createdBy
        };

        if (!checkpointType.Private) {
            @case.PublicCheckpointId = nextCheckpoint.Id;
        }

        if (checkpointType.Status == CaseStatus.Completed && @case.CompletedBy is null) {
            @case.CompletedBy = createdBy;
            // fast-forward public data Id
            @case.PublicDataId = @case.DataId;
            // force remove assignment (if any)
            @case.AssignedTo = null;
        }

        @case.CheckpointId = nextCheckpoint.Id;
        @case.Checkpoints.Add(nextCheckpoint);
        await DbContext.Checkpoints.AddAsync(nextCheckpoint);
    }

    private async Task AddCaseData(AuditMeta createdBy, DbCase @case, dynamic data) {
        if (data is null) throw new ArgumentNullException(nameof(data));

        // Validate data against case type json schema, only when schema is present
        if (@case.CaseType.DataSchema is not null && !_schemaValidator.IsValid(@case.CaseType.DataSchema, (object)data)) {
            throw new BusinessException("Data validation error.");
        }

        var newDataVersion = new DbCaseData {
            Case = @case,
            CreatedBy = createdBy,
            Data = data
        };

        @case.DataId = newDataVersion.Id;
        
        // Update checkpoint data
        await DbContext.CaseData.AddAsync(newDataVersion);
    }
}