using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Events;

/// <summary>Published after the back-office sets the permission of the owner of a case, or updates one of its co-owners.</summary>
public class CaseContactPermissionChangedEvent : ICaseEvent
{
    /// <summary>The case identifier.</summary>
    public Guid CaseId { get; set; }
    /// <summary>The user id of the owner or co-owner.</summary>
    public string? UserId { get; set; }
    /// <summary>The permission the owner or co-owner has from now on.</summary>
    public ContactPermission Permission { get; set; }
    /// <summary>The user that made the change, and when.</summary>
    public AuditMeta ChangedBy { get; set; } = null!;
}
