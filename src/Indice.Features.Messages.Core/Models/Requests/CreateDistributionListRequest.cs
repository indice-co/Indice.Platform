namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>Models a request when creating a distribution list.</summary>
public class CreateDistributionListRequest
{
    /// <summary>The name of the distribution list.</summary>
    public string Name { get; set; }

    /// <summary>Indicates that the list is generated through a system process.</summary>
    internal bool IsSystemGenerated { get; set; }
}
