namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>Options used to filter the distribution lists.</summary>
public class DistributionListFilter
{
    /// <summary>Indicates that the list is system generated.</summary>
    public bool? IsSystemGenerated { get; set; }
}
