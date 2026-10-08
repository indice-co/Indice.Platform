using Microsoft.Data.SqlTypes;

namespace Indice.Features.Agents.Core.Data;

/// <summary>
/// A chunk of a document, used for RAG retrieval. Each chunk is associated with a parent <see cref="DbDocument"/>
/// and contains a portion of the document's content along with metadata for indexing and retrieval.
/// </summary>
public class DbChunk
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key → <see cref="DbDocument.Id"/>.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>Chunk text.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Optional title (nearest heading or document title) used for display.</summary>
    public string? Title { get; set; }

    /// <summary>Denormalized source from the parent document; speeds up retrieval display.</summary>
    public string? Source { get; set; }

    /// <summary>Zero-based order within the parent document.</summary>
    public int ChunkIndex { get; set; }

    /// <summary>SHA-256 of the chunk content; drives selective re-embedding.</summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>Optional category used as a retrieval filter (denormalized from document).</summary>
    public string? Category { get; set; }

    /// <summary>Optional ISO language code used as a retrieval filter (denormalized from document).</summary>
    public string? Language { get; set; }

    /// <summary>Optional page number for sources that have pagination.</summary>
    public int? SourcePage { get; set; }

    /// <summary>Heading breadcrumb (e.g. <c>H1 &gt; H2 &gt; H3</c>) for citation rendering.</summary>
    public string? HeadingPath { get; set; }

    /// <summary>Dense embedding for similarity search.</summary>
    public SqlVector<float> Embedding { get; set; }

    /// <summary>Embedding model used to vectorize this chunk (denormalized for re-embedding migrations).</summary>
    public string EmbeddingModel { get; set; } = string.Empty;

    /// <summary>Token count of <see cref="Content"/> under the embedding model's tokenizer; drives context-window budgeting.</summary>
    public int TokenCount { get; set; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Navigation: parent document.</summary>
    public virtual DbDocument Document { get; set; } = null!;
}
