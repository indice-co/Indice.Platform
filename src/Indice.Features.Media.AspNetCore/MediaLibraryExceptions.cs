using Indice.Types;

namespace Indice.Features.Media.AspNetCore;

/// <summary>Exception thrown from Media Library API feature.</summary>
public class MediaLibraryExceptions
{
    /// <summary>Folder not found exception.</summary>
    /// <param name="id">The folder id.</param>
    public static BusinessException FolderNotFound(Guid id) => new($"Folder with Id: '{id}' does not exist.", nameof(FolderNotFound), new string[] { $"Folder with Id: '{id}' does not exist." });
    /// <summary>File not found exception.</summary>
    /// <param name="id">The file id.</param>
    public static BusinessException FileNotFound(Guid id) => new($"File with Id: '{id}' does not exist.", nameof(FileNotFound), new string[] { $"File with Id: '{id}' does not exist." });
}
