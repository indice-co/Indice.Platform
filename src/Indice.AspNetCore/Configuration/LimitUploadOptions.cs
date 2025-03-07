using Indice.AspNetCore.Filters;

namespace Indice.AspNetCore.Configuration;

/// <summary>Options for the <see cref="AllowedFileSizeAttribute"/>.</summary>
public class LimitUploadOptions
{
    /// <summary>Configures the file size limit. Default is 4 MB.</summary>
    public long DefaultMaxFileSizeBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>Configures the permitted file extensions.</summary>
    public HashSet<string> DefaultAllowedFileExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".bmp", ".svg", ".webp"];
}
