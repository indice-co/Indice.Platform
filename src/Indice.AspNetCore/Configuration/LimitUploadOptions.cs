using Indice.AspNetCore.Filters;

namespace Indice.AspNetCore.Configuration;

/// <summary>Options for the <see cref="AllowedFileSizeAttribute"/>.</summary>
public class LimitUploadOptions
{
    /// <summary>Configures the file size limit. Default is 6MB.</summary>
    public long DefaultMaxFileSizeBytes { get; set; } = 6291456;

    /// <summary>Configures the permitted file extensions.</summary>
    public IReadOnlyCollection<string> DefaultAllowedFileExtensions { get; set; } = new HashSet<string> { ".pdf", ".jpeg", ".jpg", ".tif", ".tiff" };
}
