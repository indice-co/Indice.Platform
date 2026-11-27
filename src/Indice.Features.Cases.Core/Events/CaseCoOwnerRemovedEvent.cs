using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Events;

/// <summary>Published after a co-owner is removed from a case.</summary>
public class CaseCoOwnerRemovedEvent : ICaseEvent
{
    /// <summary>The case identifier.</summary>
    public Guid CaseId { get; set; }
    /// <summary>The user id of the co-owner that was removed.</summary>
    public string? UserId { get; set; }
    /// <summary>The user that removed the co-owner, and when.</summary>
    public AuditMeta RemovedBy { get; set; } = null!;
}
