namespace Indice.Features.Agents.Core.Data;

/// <summary>
/// Represents a citation of a chunk in the context of a RAG (Retrieval-Augmented Generation) workflow. 
/// Each citation includes a reference to the chunk, its score, and identifiers for the associated session and message.
/// </summary>
public class DbCitation
{
    /// <summary>The number of the citation. Used to order citations within a session.</summary>
    public int Number { get; set; }
    /// <summary>The score of the citation, indicating its relevance or quality.</summary>
    public double Score { get; set; }
    /// <summary>The identifier of the chunk being cited.</summary>
    public Guid ChunkId { get; set; }
    /// <summary>The identifier of the session in which the citation was made.</summary>
    public Guid SessionMessageId { get; set; }
    /// <summary>Navigation property to the associated chunk.</summary>
    public virtual DbChunk Chunk { get; set; } = null!;
}
