using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Events;

/// <summary>Published after a co-owner is added to a case.</summary>
public class CaseCoOwnerAddedEvent : ICaseEvent
{
    /// <summary>The case identifier.</summary>
    public Guid CaseId { get; set; }
    /// <summary>The user id of the co-owner that was added.</summary>
    public string? UserId { get; set; }
    /// <summary>The permission given to the co-owner.</summary>
    public ContactPermission Permission { get; set; }
    /// <summary>The user that added the co-owner, and when.</summary>
    public AuditMeta AddedBy { get; set; } = null!;
}
