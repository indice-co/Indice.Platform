namespace Indice.Features.Agents.Core.Models;

/// <summary>A <see cref="Chunk"/> paired with a relevance score (cosine similarity from retrieval, 0..1 from rerank).</summary>
public class RetrievedChunk : Chunk
{
    /// <summary>Relevance score. Cosine similarity (1 - distance) for retrieval; 0..1 for rerank.</summary>
    public double Score { get; init; }
}
