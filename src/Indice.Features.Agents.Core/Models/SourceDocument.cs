namespace Indice.Features.Agents.Core.Models;

/// <summary>Represents a source document.</summary>
public class SourceDocument
{
    /// <summary>The unique identifier of the source document.</summary>
    public Guid Id { get; set; }
    /// <summary>The source uri/alias of the source document.</summary>
    public string Source { get; set; } = null!;
    /// <summary>Content hash of the source document (used for dedup/versioning).</summary>
    public string ContentHash { get; set; } = null!;
    /// <summary>The content type of the source document.</summary>
    public string ContentType { get; set; } = null!;
    /// <summary>The length of the source document.</summary>
    public long ContentLength { get; set; }
    /// <summary>The name of the source document file.</summary>
    public string FileName { get; set; } = null!;
    /// <summary>Timestamp indicating when the source document was last modified.</summary>
    public DateTimeOffset? LastModified { get; set; }
    /// <summary>The binary data of the source document.</summary>
    public byte[]? Data { get; set; }
    /// <summary>Indicates whether the document is private and should not be exposed to unauthorized users.</summary>
    public bool IsPrivate { get; set; }
}

/// <summary>
/// Represents a link to a source document without including the binary data.
/// </summary>
public class SourceDocumentLink
{
    /// <summary>The unique identifier of the source document.</summary>
    public Guid Id { get; set; }
    /// <summary>The type of the source document.</summary>
    public string ContentHash { get; set; } = null!;
    /// <summary>The content type of the source document.</summary>
    public string ContentType { get; set; } = null!;
    /// <summary>The length of the source document.</summary>
    public long Length { get; set; }
    /// <summary>The original filename of the source document file.</summary>
    public string FileName { get; set; } = null!;
    /// <summary>The title of the source document.</summary>
    public string SourceTitle { get; set; } = null!;
    /// <summary>The absolute URL to retrieve the source document.</summary>
    public string SourceUrl { get; set; } = null!;
    /// <summary>Indicates whether the document is private and should not be exposed to unauthorized users.</summary>
    public bool IsPrivate { get; set; }
}
