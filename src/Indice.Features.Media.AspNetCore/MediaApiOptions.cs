using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Media.AspNetCore;

/// <summary>Options for configuring the API for media management.</summary>
public class MediaApiOptions
{
    /// <summary>Creates a new instance of <see cref="MediaApiOptions"/>.</summary>
    public MediaApiOptions() { }

    /// <summary>Creates a new instance of <see cref="MediaApiOptions"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MediaApiOptions(IServiceCollection services) {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }
    internal IServiceCollection Services { get; set; } = null!;

    /// <summary>Specifies a prefix for the media API endpoints.</summary>
    public PathString PathPrefix { get; set; } = "/";

    /// <summary>The scope name to be used for media API. Defaults to <i>media</i>.</summary>
    public string Scope { get; set; } = MediaLibraryApi.Scope;

    /// <summary>The authentication scheme to be used for media API. Defaults to <i>Bearer</i>.</summary>
    public string AuthenticationScheme { get; set; } = MediaLibraryApi.AuthenticationScheme;

    /// <summary>The maximum acceptable size of the files to be uploaded. Defaults to <i>10MB</i>.</summary>
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>The File deletion policy. Defaults to <i>true</i></summary>
    public bool UseSoftDelete { get; set; } = true;

    /// <summary>The acceptable file extensions. Defaults to <i>.png, .jpg, .gif, .webp</i>.</summary>
    public string AcceptableFileExtensions { get; set; } = ".png, .jpg, .gif, .webp";

    private readonly HashSet<int> _AllowedThumbnailSizes = [24, 32, 48, 64, 128, 192, 256, 512];
    /// <summary>Allowed tile sizes. Only these sizes are available.</summary>
    public ICollection<int> AllowedThumbnailSizes => _AllowedThumbnailSizes;

    /// <summary>The schema name to be used for media Db. Defaults to <i>media</i>.</summary>
    public string DatabaseSchema { get; set; } = "media";

    /// <summary>
    /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
    /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:MediaLibraryDbConnection</i> to be present.
    /// </summary>
    public Action<IServiceProvider, DbContextOptionsBuilder>? ConfigureDbContext { get; set; }
}