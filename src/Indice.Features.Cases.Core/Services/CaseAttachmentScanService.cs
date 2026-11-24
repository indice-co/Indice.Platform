using Indice.Events;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Events;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Cases.Core.Services;

internal class CaseAttachmentScanService(CasesDbContext db, IPlatformEventService events, ILogger<CaseAttachmentScanService> logger) : ICaseAttachmentScanService
{
    public async Task<bool> SetStatus(Guid attachmentId, AttachmentScanStatus status, string scannerId) {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        ArgumentException.ThrowIfNullOrWhiteSpace(scannerId);
        var attachment = await db.Attachments.SingleOrDefaultAsync(x => x.Id == attachmentId);
        if (attachment is null) return false;
        if (attachment.ScanStatus == status) return true;
        var previousStatus = attachment.ScanStatus;
        var changedAt = DateTimeOffset.UtcNow;
        attachment.ScanStatus = status;
        attachment.ScanStatusChangedAt = changedAt;
        attachment.ScanStatusChangedBy = scannerId;
        await db.SaveChangesAsync();
        logger.LogInformation("Attachment {AttachmentId} scan status changed from {PreviousStatus} to {Status} by {ScannerId}", attachmentId, previousStatus, status, scannerId);
        await events.Publish(new CaseAttachmentScannedEvent {
            CaseId = attachment.CaseId, AttachmentId = attachmentId, PreviousStatus = previousStatus,
            Status = status, ScannerId = scannerId, ChangedAt = changedAt
        });
        return true;
    }
}
