using Indice.Features.Media.AspNetCore.Endpoints;
using Indice.Features.Media.AspNetCore.Models;
using Indice.Features.Media.AspNetCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;

namespace Indice.Features.Media.AspNetCore;

/// <summary>
/// Resolves the media base absolute base uri using the current <see cref="HttpContext"/> and any settings found in the <seealso cref="IMediaSettingService"/> 
/// </summary>
public class MediaBaseHrefResolver
{

    /// <summary>
    /// Resolves the media base absolute base uri using the current <see cref="HttpContext"/> and any settings found in the <see cref="IMediaSettingService"/>
    /// </summary>
    /// <param name="configuration">The configuration</param>
    /// <param name="linkGenerator">The link generator</param>
    /// <param name="serviceProvider">Will be used to resolve the optional service for media settings. The settings service for retrieving the CDN key value if found</param>
    public MediaBaseHrefResolver(IConfiguration configuration, LinkGenerator linkGenerator, IServiceProvider serviceProvider) {
        Uri baseUri = new(configuration.GetHost());
        Uri basePath = new(linkGenerator.GetPathByName(new DefaultHttpContext(), nameof(MediaHandlers.DownloadFile), values: null)!, UriKind.RelativeOrAbsolute);
        ActualBaseHref = new(baseUri, basePath);
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// The Actual media baseHref
    /// </summary>
    protected Uri ActualBaseHref { get; }
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Resolves the base path at runtime taking into account that the cdn Endpoint 
    /// may be already set and we might need to update the base path to use that.
    /// </summary>
    /// <returns>The media base href absolute path</returns>
    public async Task<Uri> ResolveBaseHrefAsync() {
        using var scope = _serviceProvider.CreateScope();
        var mediaSettingsService = scope.ServiceProvider.GetService<IMediaSettingService>();
        if (mediaSettingsService is not null) {
            var cdnUrl = await mediaSettingsService.GetSetting(MediaSetting.CDN.Key);
            if (!string.IsNullOrEmpty(cdnUrl?.Value)) {
                return new Uri(cdnUrl.Value);
            }
        }
        return ActualBaseHref;
    }
}
