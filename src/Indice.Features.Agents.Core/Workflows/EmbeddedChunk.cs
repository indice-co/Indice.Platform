namespace Indice.Features.Agents.Core.Workflows;

/// <summary>A <see cref="DocumentChunk"/> paired with the embedding vector produced by the embedding generator.</summary>
public class EmbeddedChunk
{
    /// <summary>The source chunk (text, hash, heading metadata, token count).</summary>
    public DocumentChunk Chunk { get; init; } = default!;

    /// <summary>Dense embedding aligned with the configured embedding model.</summary>
    public ReadOnlyMemory<float> Embedding { get; init; }
}
