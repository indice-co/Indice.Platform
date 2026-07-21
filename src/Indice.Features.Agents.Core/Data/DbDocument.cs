using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Data;

/// <summary>A logical source document. Owns one or more <see cref="DbChunk"/> rows.</summary>
public class DbDocument
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Human-readable title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Origin of the document (file path or URI).</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Optional category used as a retrieval filter.</summary>
    public string? Category { get; set; }

    /// <summary>Optional ISO language code (e.g. <c>en</c>, <c>el</c>) used as a retrieval filter.</summary>
    public string? Language { get; set; }

    /// <summary>SHA-256 of the normalized document content; idempotency key for re-ingestion.</summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>Embedding model used to vectorize this document's chunks.</summary>
    public string EmbeddingModel { get; set; } = string.Empty;

    /// <summary>Dimensionality of the embedding vectors stored on this document's chunks.</summary>
    public int EmbeddingDimensions { get; set; }

    /// <summary>Lifecycle status.</summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    /// <summary>Total number of chunks produced by ingestion.</summary>
    public int ChunkCount { get; set; }

    /// <summary>Timestamp at which the document was last ingested.</summary>
    public DateTimeOffset IngestedAt { get; set; }

    /// <summary>Navigation: optional binary payload and file metadata. Not loaded by default.</summary>
    public DbDocumentBlob? Blob { get; set; }
    /// <summary>Indicates whether the document is private and should not be exposed to unauthorized users.</summary>
    public bool IsPrivate { get; set; }
}
