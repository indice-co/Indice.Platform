namespace Indice.Features.Media.AspNetCore.Data.Models;

/// <summary>The Folder entity.</summary>
public class DbFolder : DbAuditableEntity
{
    /// <summary>The unique identifier of the folder.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>The folder's name.</summary>
    public required string Name { get; set; }
    /// <summary>A short description about the folder.</summary>
    public string? Description { get; set; }
    /// <summary>Foreign folder's parent <see cref="DbFolder"/>.</summary>
    public Guid? ParentId { get; set; }
    /// <summary>The parent folder.</summary>
    public DbFolder? Parent { get; set; }
    /// <summary>Marks a folder as deleted.</summary>
    public bool IsDeleted { get; set; }
    /// <summary>The contained folders.</summary>
    public ICollection<DbFolder> SubFolders { get; set; } = new List<DbFolder>();
    /// <summary>The contained files.</summary>
    public ICollection<DbFile> Files { get; set; } = new List<DbFile>();
}
