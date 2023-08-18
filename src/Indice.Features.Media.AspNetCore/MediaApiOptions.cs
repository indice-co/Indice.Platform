using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Media.AspNetCore;

/// <summary>Options for configuring the API for media management.</summary>
public class MediaApiOptions
{
    private string _apiPrefix = "/api";
    private string _apiScope = MediaLibraryApi.Scope;
    private string _authenticationScheme = MediaLibraryApi.AuthenticationScheme;
    private long _maxSize = 100 * 1024 * 1024;
    private string _acceptableFileExtensions = ".png, .jpg, .gif, .pdf, .xlsx, .docx, .pptx, .svg";
    private bool _useSoftDelete = true;

    /// <summary>Creates a new instance of <see cref="MediaApiOptions"/>.</summary>
    public MediaApiOptions() { }

    /// <summary>Creates a new instance of <see cref="MediaApiOptions"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MediaApiOptions(IServiceCollection services) {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }
    internal IServiceCollection? Services { get; set; }

    /// <summary>Specifies a prefix for the media API endpoints.</summary>
    public PathString ApiPrefix {
        get => _apiPrefix;
        set { _apiPrefix = string.IsNullOrWhiteSpace(value) ? string.Empty : value; }
    }

    /// <summary>The default scope name to be used for media API. Defaults to <i>media</i>.</summary>
    public string ApiScope {
        get => _apiScope;
        set { _apiScope = !string.IsNullOrWhiteSpace(value) ? value : throw new ValidationException("Please specify an API scope for Media API."); }
    }

    /// <summary>The default scope name to be used for media API. Defaults to <i>Bearer</i>.</summary>
    public string AuthenticationScheme {
        get => _authenticationScheme;
        set { _authenticationScheme = !string.IsNullOrWhiteSpace(value) ? value : throw new ValidationException("Please specify an authentication scheme for Media API."); }
    }

    /// <summary>The default maximum acceptable size of the files to be uploaded. Defaults to <i>10MB</i>.</summary>
    public long MaxFileSize {
        get => _maxSize;
        set { _maxSize = value; }
    }

    /// <summary>The default File delete policy. Defaults to <i>true</i></summary>
    public bool UseSoftDelete {
        get => _useSoftDelete;
        set { _useSoftDelete = value; }
    }

    /// <summary>The default acceptable file extensions. Defaults to <i>.png, .jpg, .gif</i>.</summary>
    public string AcceptableFileExtensions {
        get => _acceptableFileExtensions;
        set { _acceptableFileExtensions = !string.IsNullOrWhiteSpace(value) ? value : throw new ValidationException("Please specify an the acceptable file extensions."); }
    }


    /// <summary>
    /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
    /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:CampaignsDbConnection</i> to be present.
    /// </summary>
    public Action<IServiceProvider, DbContextOptionsBuilder>? ConfigureDbContext { get; set; }
}