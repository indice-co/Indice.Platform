using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Assets;

/// <summary>Post configuration for <see cref="StaticFileOptions"/> that changes the file provider of <see cref="StaticFileMiddleware"/> to also serve identity UI assets.</summary>
public class IdentityUIStaticFileConfigureOptions : IPostConfigureOptions<StaticFileOptions>
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    /// <summary>Creates a new instance of <see cref="IdentityUIStaticFileConfigureOptions"/>.</summary>
    /// <param name="webHostEnvironment">Provides information about the web hosting environment an application is running in.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public IdentityUIStaticFileConfigureOptions(IWebHostEnvironment webHostEnvironment) {
        _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
    }

    /// <inheritdoc />
    public void PostConfigure(string? name, StaticFileOptions options) {
        if (options is null) {
            throw new ArgumentNullException(nameof(options));
        }
        if (options.FileProvider is null && _webHostEnvironment.WebRootFileProvider is null) {
            throw new InvalidOperationException("Not configured file providers.");
        }
        options.ContentTypeProvider ??= new FileExtensionContentTypeProvider();
        options.FileProvider = new CompositeFileProvider(
            options.FileProvider ?? _webHostEnvironment.WebRootFileProvider,
            new ManifestEmbeddedFileProvider(GetType().Assembly, "wwwroot")
        );
    }
}
