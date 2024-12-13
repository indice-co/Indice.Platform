namespace Indice.Features.Messages.Core.Data.Models;

/// <summary></summary>
public abstract class DbAuditableEntity
{
    /// <summary>Specifies the principal that created the entity.</summary>
    public string? CreatedBy { get; set; }
    /// <summary>Specifies when an entity was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Specifies the principal that update the entity.</summary>
    public string? UpdatedBy { get; set; }
    /// <summary>Specifies when an entity was updated.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
