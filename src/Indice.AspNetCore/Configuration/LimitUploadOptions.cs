using Indice.AspNetCore.Filters;

namespace Indice.AspNetCore.Configuration;

/// <summary>Options for the <see cref="AllowedFileSizeAttribute"/>.</summary>
public class LimitUploadOptions
{
    /// <summary>Configures the file size limit. Default is 4 MB.</summary>
    public long DefaultMaxFileSizeBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>Configures the permitted file extensions.</summary>
    public HashSet<string> DefaultAllowedFileExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".bmp", ".svg", ".webp"];

    /// <summary>
    /// When set to <see langword="true"/>, uploaded files are also validated against their expected magic bytes (file signatures).
    /// Files whose content does not match the declared extension will be rejected. Default is <see langword="false"/>.
    /// </summary>
    public bool EnableMagicByteValidation { get; set; } = false;
}
