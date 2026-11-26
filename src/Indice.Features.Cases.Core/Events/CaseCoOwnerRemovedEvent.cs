using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Events;

/// <summary>Published after a co-owner is removed from a case.</summary>
public class CaseCoOwnerRemovedEvent : ICaseEvent
{
    /// <summary>The case identifier.</summary>
    public Guid CaseId { get; set; }
    /// <summary>The code of the case type.</summary>
    public string CaseTypeCode { get; set; } = null!;
    /// <summary>The co-owner that was removed, with the permission held until then.</summary>
    public ContactMeta CoOwner { get; set; } = null!;
    /// <summary>The user that removed the co-owner, and when.</summary>
    public AuditMeta RemovedBy { get; set; } = null!;
}
