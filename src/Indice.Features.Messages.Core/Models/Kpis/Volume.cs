namespace Indice.Features.Messages.Core.Models.Kpis;

/// <summary>Represents the volume of messages for a specific entity type.</summary>
public class Volume<TEntity> where TEntity : class, new()
{
    /// <summary>
    /// The voume of messages for the specified entity type
    /// </summary>
    public int Total { get; set; }
    /// <summary>The entity information the volume of messages is aggregated for</summary>
    public TEntity Info { get; set; } = new();
}
