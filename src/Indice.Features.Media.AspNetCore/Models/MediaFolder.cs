namespace Indice.Features.Media.AspNetCore.Models;

/// <summary> The folder object.</summary>
public class MediaFolder
{
    /// <summary>The Id of the Folder.</summary>
    public Guid Id { get; set; }
    /// <summary>The Name of the Folder.</summary>
    public required string Name { get; set; }
    /// <summary>The Path to the Folder.</summary>
    public required string Path { get; set; }
    /// <summary>The Description of the Folder.</summary>
    public string? Description { get; set; }
    /// <summary>Specifies the principal that created the entity.</summary>
    public required string CreatedBy { get; set; }
    /// <summary>Specifies when an entity was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Specifies the principal that update the entity.</summary>
    public string? UpdatedBy { get; set; }
    /// <summary>Specifies when an entity was updated.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
    /// <summary>The Id of the Parent Folder.</summary>
    public Guid? ParentId { get; set; }
    /// <summary>The number of contained Folders.</summary>
    public int? SubFoldersCount { get; set; }
    /// <summary>The number of contained Files.</summary>
    public int? FilesCount { get; set; }

    internal bool IsDeleted { get; set; }
}
