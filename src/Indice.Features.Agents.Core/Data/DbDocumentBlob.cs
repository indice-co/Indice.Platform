namespace Indice.Features.Agents.Core.Data;

/// <summary>Optional binary payload and file metadata for a <see cref="DbDocument"/>.</summary>
/// <remarks>Shares the primary key with <see cref="DbDocument"/> and is not loaded by default.</remarks>
public class DbDocumentBlob
{
    /// <summary>Primary key – shared with the parent <see cref="DbDocument"/>.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>Original file name including extension (e.g. <c>report.pdf</c>).</summary>
    public string FileName { get; set; } = null!;

    /// <summary>MIME / media type of the file (e.g. <c>application/pdf</c>).</summary>
    public string ContentType { get; set; } = null!;

    /// <summary>Size of the file in bytes.</summary>
    public long ContentLength { get; set; }

    /// <summary>Timestamp indicating when the document blob was last modified. This is used for concurrency control and to track changes to the blob data.</summary>
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Entity tag for the document blob. This is used for concurrency control and caching.</summary>
    public string ETag { get; set; } = null!;

    /// <summary>Raw file bytes; <see langword="null"/> when the content is stored externally.</summary>
    public byte[]? Data { get; set; }

    /// <summary>Navigation: parent document.</summary>
    public DbDocument Document { get; set; } = null!;

}
