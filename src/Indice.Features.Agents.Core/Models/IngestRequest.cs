namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Represents a request to ingest a document into the system. Contains information about the document's content, source, and metadata.
/// </summary>
public class IngestRequest
{
    /// <summary>Opens a UTF-8 text based content stream. Can consume markdown.</summary>
    /// <remarks>This is used as the tokenization input</remarks>
    public Func<Stream> OpenContentStream { get; set; } = null!;
    /// <summary>Opens the source file for storage purposes. (Optional)</summary>
    public Func<Stream>? OpenSourceStream { get; set; }
    /// <summary>Actual source url/uri</summary>
    /// <remarks>Used for provenance and traceability. Can be an external URL or an internal URI.</remarks>
    public string Source { get; set; } = null!;
    /// <summary>Original filename</summary>
    public string FileName { get; set; } = null!;
    /// <summary>Original conent type</summary>
    public string ContentType { get; set; } = null!;
    /// <summary>Original content length</summary>
    public long ContentLength { get; set; }
    /// <summary>Category</summary>
    public string? Category { get; set; }
    /// <summary>Two letter language iso code</summary>
    public string? Language { get; set; }
}
