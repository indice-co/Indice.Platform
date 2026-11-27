using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Events;

/// <summary>Published after an attachment scan status changes.</summary>
public class CaseAttachmentScannedEvent : ICaseEvent
{
    /// <summary>The case identifier.</summary>
    public Guid CaseId { get; set; }
    /// <summary>The attachment identifier.</summary>
    public Guid AttachmentId { get; set; }
    /// <summary>The previous status.</summary>
    public AttachmentScanStatus PreviousStatus { get; set; }
    /// <summary>The new status.</summary>
    public AttachmentScanStatus Status { get; set; }
    /// <summary>The trusted scanner identity.</summary>
    public string ScannerId { get; set; } = null!;
    /// <summary>The time of the status change.</summary>
    public DateTimeOffset ChangedAt { get; set; }
}
