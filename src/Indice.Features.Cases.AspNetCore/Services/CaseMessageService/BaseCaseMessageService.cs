using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Events;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services.CaseMessageService
{
    internal abstract class BaseCaseMessageService
    {
        private readonly CasesDbContext _dbContext;
        private readonly ICaseEventService _caseEventService;
        private readonly ISchemaValidator _schemaValidator;

        protected BaseCaseMessageService(
            CasesDbContext dbContext,
            ICaseEventService caseEventService,
            ISchemaValidator schemaValidator) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _caseEventService = caseEventService ?? throw new ArgumentNullException(nameof(caseEventService));
            _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        }

        protected async Task<Guid?> SendInternal(DbCase @case, Message message, ClaimsPrincipal user) {
            Guid? attachmentId = null;
            var caseId = @case.Id;
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (message.File == null && message.CheckpointTypeName == null && message.Comment == null && message.Data == null) {
                return attachmentId;
            }
            
            var caseType = await _dbContext.CaseTypes.FindAsync(@case.CaseTypeId);
            if (caseType == null) throw new ArgumentNullException(nameof(caseType));

            await _caseEventService.Publish(new CaseMessageCreatedEvent(caseId, message));

            var newCheckpointType = await _dbContext.CheckpointTypes
                .AsQueryable()
                .SingleOrDefaultAsync(x => x.Code == $"{caseType.Code}:{message.CheckpointTypeName}");
            if (message.ReplyToCommentId.HasValue) {
                var exists = await _dbContext.Comments.AsQueryable().AnyAsync(x => x.CaseId == caseId && x.Id == message.ReplyToCommentId.Value);
                if (!exists) {
                    throw new Exception("Invalid reply to comment id. Not found on the current case.");
                }
            }
            if (message.File == null && message.CheckpointTypeName == null && message.Data == null) {
                await AddComment(user, caseId, message.Comment, message.ReplyToCommentId, message.PrivateComment);
            } else if (message.File != null && message.CheckpointTypeName == null) {
                var attachment = await AddAttachment(user, @case, message.Comment, message.File);
                attachmentId = attachment.Id;
            } else if (message.File == null && message.CheckpointTypeName != null) {
                var checkpoint = await AddCheckpoint(user, @case, newCheckpointType);
                if (!string.IsNullOrWhiteSpace(message.Comment)) {
                    message.PrivateComment ??= newCheckpointType.Private;
                    await AddComment(user, caseId, message.Comment, message.ReplyToCommentId, message.PrivateComment);
                }
            } else if (!string.IsNullOrEmpty(message.Data)) {
                await AddCaseData(user, @case, message.Data);
                if (!string.IsNullOrWhiteSpace(message.Comment)) {
                    await AddComment(user, caseId, message.Comment, message.ReplyToCommentId, message.PrivateComment);
                }
            } else {
                await AddCheckpoint(user, @case, newCheckpointType);
                var attachment = await AddAttachment(user, @case, string.Empty, message.File!);
                attachmentId = attachment.Id;
            }
            if (!string.IsNullOrEmpty(message.CheckpointTypeName)) {
                if (!message.PrivateComment.HasValue) {
                    message.PrivateComment = newCheckpointType.Private;
                }
                var suffix = $"Case status changed to '{newCheckpointType.Code}'";
                message.Comment = string.IsNullOrEmpty(message.Comment) ? suffix : $"{message.Comment}. {suffix}";
            }

            await _dbContext.SaveChangesAsync();
            await _caseEventService.Publish(new CaseMessageSentEvent(caseId, message));
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

        private async Task AddComment(ClaimsPrincipal user, Guid caseId, string text, Guid? replyToCommentId, bool? @private) {
            var newComment = new DbComment {
                IsCustomer = false, // todo decide if customer, from claims
                Private = @private ?? true,
                CaseId = caseId,
                Text = text,
                ReplyToCommentId = replyToCommentId,
                CreatedBy = AuditMeta.Create(user)
            };

            await _dbContext.Comments.AddAsync(newComment);
        }

        private async Task<CasesAttachmentLink> AddAttachment(ClaimsPrincipal user, DbCase @case, string? comment, IFormFile file) {
            var attachment = new DbAttachment {
                CaseId = @case.Id
            };
            attachment.PopulateFrom(file, @case.Id, true);
            _dbContext.Attachments.Add(attachment);
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
            await _dbContext.Comments.AddAsync(commentEntity);
            return link;
        }

        private async Task<DbCheckpoint> AddCheckpoint(ClaimsPrincipal user, DbCase @case, DbCheckpointType checkpointType) {
            var lastCheckpoint = await _dbContext.Checkpoints
                .Include(p => p.CheckpointType)
                .Where(x => x.CaseId == @case.Id)
                .OrderByDescending(x => x.CreatedBy.When)
                .FirstOrDefaultAsync();

            // If the new checkpoint is the same as the last attempt, only add the comment. 
            if (lastCheckpoint != null && lastCheckpoint.CheckpointType.Code == checkpointType.Code) {
                return lastCheckpoint;
            }

            // Else continue to change the checkpoint.
            if (lastCheckpoint != null) {
                lastCheckpoint.CompletedDate = DateTimeOffset.UtcNow;
            }

            var newCheckpoint = new DbCheckpoint {
                CaseId = @case.Id,
                CheckpointTypeId = checkpointType.Id,
                CreatedBy = AuditMeta.Create(user)
            };

            if (!checkpointType.Private) {
                @case.PublicCheckpointId = newCheckpoint.Id;
            }

            if (checkpointType.PublicStatus == CasePublicStatus.Completed) {
                @case.CompletedBy = AuditMeta.Create(user);
            }

            @case.Checkpoints.Add(newCheckpoint);
            await _dbContext.Checkpoints.AddAsync(newCheckpoint);
            return newCheckpoint;
        }

        private async Task AddCaseData(ClaimsPrincipal user, DbCase @case, string data) {
            if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(nameof(data));

            // Validate data against case type json schema
            if (!_schemaValidator.IsValid(@case.CaseType.DataSchema, data)) {
                throw new Exception("Data validation error."); // todo proper exception handling (BadRequest)
            }

            // Update checkpoint data
            await _dbContext.CaseData.AddAsync(new DbCaseData {
                Case = @case,
                CreatedBy = AuditMeta.Create(user),
                Data = data
            });
        }
    }
}