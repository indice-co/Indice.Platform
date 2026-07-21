using Indice.Features.Agents.Core.Services;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>Document metadata at the store boundary — what <see cref="IDocumentsService.ReplaceAsync"/> needs to write a <see cref="Data.DbDocument"/> row.</summary>
public class IngestedDocument
{
    /// <summary>Display title. Derived from the uploaded file name (without extension).</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Stable identifier within the store — set to the uploaded file name.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Document-level category — the first <c>#</c> heading in the file, or the form-data <c>category</c> when the file has none.</summary>
    public string? Category { get; init; }

    /// <summary>ISO language code from the form-data <c>language</c> field.</summary>
    public string? Language { get; init; }

    /// <summary>SHA-256 of <c>Title|Category|Language|Body</c> (upper-hex, 64 chars). Drives re-upload dedup.</summary>
    public string ContentHash { get; init; } = string.Empty;
    /// <summary>Indicates whether the document is private and should not be exposed to unauthorized users.</summary>
    public bool IsPrivate { get; set; }
    /// <summary>MIME / media type of the file.</summary>
    public string ContentType { get; set; } = null!;
    /// <summary>Size of the file in bytes.</summary>
    public long ContentLength { get; set; }
    /// <summary>Original filename.</summary>
    public string FileName { get; set; } = null!;
    /// <summary>Binary data of the file. Not loaded by default.</summary>
    public byte[]? FileData { get; set; }
}
