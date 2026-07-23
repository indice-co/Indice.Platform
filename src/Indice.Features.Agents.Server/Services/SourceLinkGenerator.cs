using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Server.Endpoints;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Agents.Server.Services;

/// <summary>Generates links to source documents.</summary>
public class SourceLinkGenerator : ISourceLinkGenerator
{
    /// <summary>Creates a new instance of <see cref="SourceLinkGenerator"/>.</summary>
    public SourceLinkGenerator(LinkGenerator linkGenerator, IConfiguration configuration) {
        LinkGenerator = linkGenerator ?? throw new ArgumentNullException(nameof(linkGenerator));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>Gets the LinkGenerator instance.</summary>
    public LinkGenerator LinkGenerator { get; }

    /// <summary>Gets the IConfiguration instance.</summary>
    public IConfiguration Configuration { get; }

    /// <inheritdoc/>
    public string GenerateLink(string sourceUrl) => 
        sourceUrl.StartsWith("local://", StringComparison.OrdinalIgnoreCase) ?
        GenerateLocalLink(sourceUrl) : 
        sourceUrl;

    private string GenerateLocalLink(string sourceUrl) {
        var link = LinkGenerator.GetPathByName(nameof(SourcesHandlers.GetActualSource), new { path = sourceUrl[8..] }) ?? throw new InvalidOperationException("Failed to generate link.");
        return Configuration.GetHost() + link;
    }
}
