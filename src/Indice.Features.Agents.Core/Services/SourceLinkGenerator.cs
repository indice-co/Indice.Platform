namespace Indice.Features.Agents.Core.Services;

/// <summary>Generates links to source documents.</summary>
public interface ISourceLinkGenerator
{
    /// <summary>Generates a link to a source document.</summary>
    string GenerateLink(string sourceUrl);
}

/// <summary>A no-op implementation of <see cref="ISourceLinkGenerator"/> that returns the source URL as-is.</summary>
public interface NoOpSourceLinkGenerator : ISourceLinkGenerator
{
    /// <inheritdoc/>
    public string GenerateLink(string sourceUrl) => sourceUrl;
}
