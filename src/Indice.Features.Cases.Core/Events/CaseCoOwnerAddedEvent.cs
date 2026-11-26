using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Events;

/// <summary>Published after a co-owner is added to a case.</summary>
public class CaseCoOwnerAddedEvent : ICaseEvent
{
    /// <summary>The case identifier.</summary>
    public Guid CaseId { get; set; }
    /// <summary>The code of the case type.</summary>
    public string CaseTypeCode { get; set; } = null!;
    /// <summary>The co-owner that was added, with the permission given.</summary>
    public ContactMeta CoOwner { get; set; } = null!;
    /// <summary>The user that added the co-owner, and when.</summary>
    public AuditMeta AddedBy { get; set; } = null!;
}
