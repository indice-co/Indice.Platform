namespace Indice.Features.Messages.Core.Models;

/// <summary>Models a distribution list.</summary>
public class DistributionList
{
    /// <summary>The unique id.</summary>
    public Guid Id { get; set; }
    /// <summary>The name of the distribution list.</summary>
    public string? Name { get; set; }
    /// <summary>Specifies the principal that created the list.</summary>
    public string? CreatedBy { get; set; }
    /// <summary>Specifies when a list was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Specifies the principal that update the list.</summary>
    public string? UpdatedBy { get; set; }
    /// <summary>Specifies when a list was updated.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
