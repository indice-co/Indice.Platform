namespace Indice.Features.Media.AspNetCore.Models;

/// <summary>Models a file.</summary>
public class MediaFile
{
    /// <summary>Creates a new instance of <see cref="MediaFile"/>.</summary>
    public MediaFile() {
    }

    /// <summary>The unique id of the file.</summary>
    public Guid Id { get; set; }
    /// <summary>The name of the file.</summary>
    public string Name { get; set; }
    /// <summary>The description of the file.</summary>
    public string? Description { get; set; }
    /// <summary>The file extension.</summary>
    public string FileExtension { get; set; }
    /// <summary>The file mime type.</summary>
    public string ContentType { get; set; }
    /// <summary>The file size.</summary>
    public int ContentLength { get; set; }
    /// <summary>The file as a byte array.</summary>
    public byte[]? Data { get; set; }
    /// <summary>The Id of the folder containing the file.</summary>
    public Guid? FolderId { get; set; }
    /// <summary>The file URI.</summary>
    public string PermaLink { get; set; }
    /// <summary>Specifies the principal that created the entity.</summary>
    public string CreatedBy { get; set; }
    /// <summary>Specifies when an entity was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Specifies the principal that update the entity.</summary>
    public string? UpdatedBy { get; set; }
    /// <summary>Specifies when an entity was updated.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
