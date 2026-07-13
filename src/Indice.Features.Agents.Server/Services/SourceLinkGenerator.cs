using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Server.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace Indice.Features.Agents.Server.Services;

/// <summary>Generates links to source documents.</summary>
public class SourceLinkGenerator : ISourceLinkGenerator
{
    /// <summary>Creates a new instance of <see cref="SourceLinkGenerator"/>.</summary>
    public SourceLinkGenerator(LinkGenerator linkGenerator) {
        LinkGenerator = linkGenerator ?? throw new ArgumentNullException(nameof(linkGenerator));
    }

    /// <summary>Gets the LinkGenerator instance.</summary>
    public LinkGenerator LinkGenerator { get; }

    /// <inheritdoc/>
    public string GenerateLink(string sourceUrl) => 
        sourceUrl.StartsWith("local://", StringComparison.OrdinalIgnoreCase) ? 
        LinkGenerator.GetPathByName(nameof(SourcesHandlers.GetActualSource), new { path = sourceUrl.Substring(8) }) ?? throw new InvalidOperationException("Failed to generate link.") : 
        sourceUrl;
}
