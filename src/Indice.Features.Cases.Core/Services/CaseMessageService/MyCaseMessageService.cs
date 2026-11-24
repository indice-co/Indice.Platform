using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Events;
using Indice.Services;
using Indice.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Services.CaseMessageService;

/// <inheritdoc />
internal class MyCaseMessageService : BaseCaseMessageService, IMyCaseMessageService
{
    private readonly CasesOptions _options;

    public MyCaseMessageService(
        CasesDbContext dbContext,
        IPlatformEventService platformEventService,
        ISchemaValidator schemaValidator,
        IOptions<CasesOptions> options,
        [FromKeyedServices(CasesCoreConstants.FileServiceKey)] IFileService? fileService = null)
        : base(dbContext, platformEventService, schemaValidator, fileService) {
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<Guid?> Send(Guid caseId, UserActor user, Message message, AuditMeta createdBy) {
        var @case = await GetMyCase(caseId, user);
        return await SendInternal(@case, message, createdBy);
    }

    public async Task AddComment(Guid caseId, UserActor user, CustomerCommentRequest request) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(request);
        if (_options.CustomerCommentMaxLength <= 0) {
            throw new InvalidOperationException("CustomerCommentMaxLength must be positive.");
        }
        if (string.IsNullOrWhiteSpace(request.Comment) || request.Comment.Length > _options.CustomerCommentMaxLength) {
            throw new BusinessException($"Comment is required and must not exceed {_options.CustomerCommentMaxLength} characters.");
        }
        var @case = await DbContext.Cases
            .Include(c => c.Checkpoint).ThenInclude(c => c.CheckpointType)
            .FirstOrDefaultAsync(c => c.Id == caseId && (c.CreatedBy.Id == user.Id || c.Owner.UserId == user.Id));
        if (@case is null || string.IsNullOrEmpty(user.Id)) {
            throw new BusinessException("Case not found.");
        }
        if (@case.Draft || @case.Checkpoint is null || @case.Checkpoint.CheckpointType is null ||
            @case.Checkpoint.CheckpointType.Status is CaseStatus.Completed or CaseStatus.Deleted) {
            throw new BusinessException("Comments are not allowed for this case in its current state.");
        }
        if (request.ReplyToCommentId is Guid replyId &&
            !await DbContext.Comments.AnyAsync(c => c.CaseId == caseId && c.Id == replyId && !c.Private)) {
            throw new BusinessException("Invalid reply to comment id. No accessible comment was found on the current case.");
        }
        if (request.AttachmentId is Guid attachmentId) {
            var attachment = await DbContext.Attachments.AsNoTracking()
                .Where(a => a.Id == attachmentId && a.CaseId == caseId)
                .Select(a => new { a.ScanStatus })
                .SingleOrDefaultAsync();
            if (attachment is null) {
                throw new BusinessException("Invalid attachment id. Not found on the current case.");
            }
            if (_options.AttachmentScanningEnabled && attachment.ScanStatus != AttachmentScanStatus.Safe) {
                throw new BusinessException("The attachment must be marked safe before it can be referenced by a customer comment.");
            }
        }
        await SendInternal(@case, new Message {
            Comment = request.Comment,
            ReplyToCommentId = request.ReplyToCommentId,
            AttachmentId = request.AttachmentId,
            PrivateComment = false
        }, AuditMeta.Create(user), isCustomerComment: true);
    }

    private async Task<DbCase> GetMyCase(Guid caseId, UserActor user) {
        if (caseId == Guid.Empty) {
            throw new ArgumentException(nameof(caseId));
        }
        var userId = user.Id;
        if (string.IsNullOrEmpty(userId)) {
            throw new ArgumentException(nameof(userId));
        }
        var @case = await DbContext.Cases.AsQueryable().FirstOrDefaultAsync(c => c.Id == caseId && c.CreatedBy.Id == userId);
        if (@case == null) {
            throw new ArgumentNullException(nameof(caseId), "Case not found");
        }
        return @case;
    }
}