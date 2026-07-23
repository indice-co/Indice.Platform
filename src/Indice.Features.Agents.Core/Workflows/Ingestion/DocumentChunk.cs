namespace Indice.Features.Agents.Core.Workflows.Ingestion;

/// <summary>A single chunk produced by the ingestion pipeline parser; carries its own content hash + heading breadcrumb.</summary>
public class DocumentChunk
{
    /// <summary>Zero-based ordinal within the source document.</summary>
    public int ChunkIndex { get; init; }

    /// <summary>Chunk text.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>SHA-256 of <see cref="Content"/> (upper-hex, 64 chars).</summary>
    public string ContentHash { get; init; } = string.Empty;

    /// <summary>Heading breadcrumb at chunk start (e.g. <c>H1 &gt; H2 &gt; H3</c>); <c>null</c> when no enclosing heading.</summary>
    public string? HeadingPath { get; init; }

    /// <summary>Nearest heading text used as a display label; <c>null</c> when the chunk starts above any heading.</summary>
    public string? Title { get; init; }

    /// <summary>Per-chunk category, set by the parser to the most recent <c>#</c> heading in scope; <c>null</c> when no <c>#</c> precedes the chunk. Denormalized onto <see cref="Data.DbChunk.Category"/>.</summary>
    public string? Category { get; init; }

    /// <summary>Token count of <see cref="Content"/>. Currently written as <c>0</c> — no live consumer slices on it.</summary>
    public int TokenCount { get; init; }
}
