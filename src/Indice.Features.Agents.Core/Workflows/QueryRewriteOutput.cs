namespace Indice.Features.Agents.Core.Workflows;

/// <summary>Output payload of <c>QueryRewriter</c>.</summary>
public class QueryRewriteOutput
{
    /// <summary>The classified intent, forwarded from upstream.</summary>
    public Intent Intent { get; init; } = new();

    /// <summary>Retrieval filters, forwarded from upstream.</summary>
    public RetrievalFilters Filters { get; init; } = new();

    /// <summary>One or more reworded versions of the original question to be embedded and searched. Always contains at least the original.</summary>
    public IReadOnlyList<string> RewrittenQueries { get; init; } = Array.Empty<string>();
}
