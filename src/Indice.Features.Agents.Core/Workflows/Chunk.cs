using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>Read-only projection of a stored chunk surfaced from the store to the RAG pipeline.</summary>
public class Chunk
{
    /// <summary>Primary key of the chunk (<c>dex.Chunks.Id</c>).</summary>
    public Guid Id { get; init; }

    /// <summary>Primary key of the parent document (<c>dex.Documents.Id</c>).</summary>
    public SourceDocumentLink Source { get; init; } = null!;

    /// <summary>Optional display title (heading or document title).</summary>
    public string? Title { get; init; }

    /// <summary>Optional heading breadcrumb, e.g. <c>H1 &gt; H2 &gt; H3</c>.</summary>
    public string? HeadingPath { get; init; }

    /// <summary>The chunk body text.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>Token count for budget tracking.</summary>
    public int TokenCount { get; init; }
}
