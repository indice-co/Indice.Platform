using Indice.AspNetCore.Filters;

namespace Indice.AspNetCore.Configuration;

/// <summary>Options for the <see cref="AllowedFileSizeAttribute"/>.</summary>
public class AllowedFileSizeAttributeOptions
{
    /// <summary>Configures the file size limit. Default is 6MB.</summary>
    public long AllowedFileSizeBytes { get; set; } = 6291456;
}
