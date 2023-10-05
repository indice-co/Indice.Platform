namespace Indice.Features.Media.AspNetCore.Models;

/// <summary>The content of a folder.</summary>
public class FolderContent
{
    /// <summary>The folder Id.</summary>
    public Guid? Id { get; set; }
    /// <summary>The folder Name.</summary>
    public string? Name { get; set; }
    /// <summary>The parent folder Id.</summary>
    public Guid? ParentId { get; set; }
    /// <summary>The contained folders.</summary>
    public List<MediaFolder> Folders { get; set; } = new List<MediaFolder>();
    /// <summary>The contained files.</summary>
    public List<MediaFile> Files { get; set; } = new List<MediaFile>();
    /// <summary>The total number of elements the folder contains.</summary>
    public int TotalCount { get; set; }

}
