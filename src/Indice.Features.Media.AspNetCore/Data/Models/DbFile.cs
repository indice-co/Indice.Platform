namespace Indice.Features.Media.AspNetCore.Data.Models;

/// <summary>The File entity.</summary>
public class DbFile : DbAuditableEntity
{
    /// <summary>The unique id of the file.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>The unique id of the file, for internal use.</summary>
    public Guid Guid { get; set; } = Guid.NewGuid();
    /// <summary>The name of the file.</summary>
    public required string Name { get; set; }
    /// <summary>The description of the file.</summary>
    public string? Description { get; set; }
    /// <summary>The file extension.</summary>
    public required string FileExtension { get; set; }
    /// <summary>The file mime type.</summary>
    public required string ContentType { get; set; }
    /// <summary>The file size.</summary>
    public int ContentLength { get; set; }
    /// <summary>The file as a byte array.</summary>
    public byte[]? Data { get; set; }
    /// <summary>The file URI.</summary>
    public string? Uri { get; set; }
    /// <summary>Foreign Key to containing folder <see cref="DbFolder"/>.</summary>
    public Guid? FolderId { get; set; }
    /// <summary>The containing folder.</summary>
    public DbFolder? Folder { get; set; }
    /// <summary>Marks a file as deleted.</summary>
    public bool IsDeleted { get; set; }
}
