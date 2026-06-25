namespace Indice.Features.Agents.Core.Models;

/// <summary>A reference to a retrieved chunk that supports an assistant answer.</summary>
public class Citation
{
    /// <summary>Primary key of the cited chunk (<c>dex.Chunks.Id</c>).</summary>
    public Guid ChunkId { get; init; }

    /// <summary>Primary key of the parent document (<c>dex.Documents.Id</c>).</summary>
    public Guid DocumentId { get; init; }

    /// <summary>Optional display title (heading or document title).</summary>
    public string? Title { get; init; }

    /// <summary>Optional heading breadcrumb for display, e.g. <c>H1 &gt; H2 &gt; H3</c>.</summary>
    public string? HeadingPath { get; init; }

    /// <summary>Relevance score (cosine similarity for retrieval, 0..1 for rerank).</summary>
    public double Score { get; init; }
}
