using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Http.Filters;

/// <summary>An optional extension to the cache key discriminator that will be created inside on the mvc CacheResourceFilter and WithParameterValidation in minimal APIs.</summary>
/// <remarks>Use only in case the default functionality is not enough.</remarks>
public interface ICacheResourceFilterKeyExtensionResolver
{
    /// <summary>Will return custom cache key extension based on the current request or null.</summary>
    /// <param name="httpContext">The current request <see cref="HttpContext"/></param>
    /// <param name="cacheKey">The current key</param>
    /// <remarks>Only return the custom/extension part of the key <b>do not recreate the whole thing</b>.</remarks>
    public Task<string> ResolveCacheKeyExtensionAsync(HttpContext httpContext, string cacheKey);
}