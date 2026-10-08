namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Campaign attachment entity.</summary>
public class DbAttachment
{
    /// <summary>Creates a new instance of <see cref="DbAttachment"/>.</summary>
    public DbAttachment() {
        Id = Guid.NewGuid();
        Guid = Guid.NewGuid();
    }

    /// <summary>The unique id of the file.</summary>
    public Guid Id { get; set; }
    /// <summary>The unique id of the file, for internal use.</summary>
    public Guid Guid { get; set; }
    /// <summary>The name of the file.</summary>
    public string Name { get; set; } = null!;
    /// <summary>The file extension.</summary>
    public string FileExtension { get; set; } = null!;
    /// <summary>The file mime type.</summary>
    public string ContentType { get; set; } = null!;
    /// <summary>The file size.</summary>
    public int ContentLength { get; set; }
    /// <summary>The file as a byte array.</summary>
    public byte[]? Data { get; set; }
    /// <summary>The file URI.</summary>
    public string? Uri { get; set; }
    /// <summary>
    /// Generates a relative file path for the campaign using the campaign's GUID and file extension.
    /// </summary>
    /// <remarks>The returned path is suitable for organizing campaign files in a directory structure based on
    /// their GUIDs. The file extension is included without a leading period, regardless of the input format.</remarks>
    /// <returns>A string representing the relative path in the format "campaigns/{prefix}/{guid}.{extension}", where the prefix
    /// is derived from the first two characters of the GUID and the extension is the file type.</returns>
    public string GetPath() => $"campaigns/{Guid.ToString("N")[..2]}/{Guid:N}.{FileExtension.TrimStart('.')}";
}
