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
}
