namespace Indice.Features.Media.AspNetCore.Data.Models;

/// <summary>Represents an auditable entity.</summary>
public abstract class DbAuditableEntity
{
    /// <summary>Specifies the principal that created the entity.</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>Specifies when an entity was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Specifies the principal that update the entity.</summary>
    public string? UpdatedBy { get; set; }
    /// <summary>Specifies when an entity was updated.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}