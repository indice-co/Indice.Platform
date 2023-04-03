#if NET7_0_OR_GREATER
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Indice.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Http.Filters;

/// <summary>Exndpoint memory caching extensions</summary>
public static class CacheResourceFilterExtensions
{
    /// <summary>
    /// Adds the ability to cache the responses.
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">the builder</param>
    /// <param name="dependentPaths">Parent paths of the current method that must be invalidated. Path template variables must match by name.</param>
    /// <param name="dependentStaticPaths">Dependent static path that must be invalidated along with this resource.</param>
    /// <param name="expiration">The absolute expiration in minutes of the cache item, expressed as an <see cref="int"/>. Defaults to 60 minutes.</param>
    /// <param name="varyByClaimType">The claim to use which value is included in the cache key.</param>
    /// <returns>The builder</returns>
    public static TBuilder WithCachedResponse<TBuilder>(this TBuilder builder, string[] dependentPaths = null, string[] dependentStaticPaths = null, int expiration = 60, string[] varyByClaimType = null) 
        where TBuilder : IEndpointConventionBuilder {
        builder.Add(eb => {
            var noCache = eb.Metadata.OfType<NoCacheMetadata>().Any();
            if (noCache)
                return;
            var cacheResourceFilterOptions = eb.ApplicationServices.GetService<IOptions<CacheResourceFilterOptions>>()?.Value ?? new CacheResourceFilterOptions();
            if (cacheResourceFilterOptions?.DisableCache == true)
                return;
            dependentPaths ??= Array.Empty<string>();
            dependentStaticPaths ??= Array.Empty<string>();
            varyByClaimType ??= Array.Empty<string>();
            eb.FilterFactories.Add((context, next) => {
                return new EndpointFilterDelegate(async (efic) => {
                    var request = efic.HttpContext.Request; 
                    var requestMethod = request.Method;

                    var cache = efic.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
                    var cacheKey = $"{request.Path}{(request.QueryString.HasValue ? request.QueryString.Value : string.Empty)}";
                    cacheKey = await AddCacheKeyDiscriminatorAsync(efic.HttpContext, varyByClaimType, cacheKey);
                    var cachedValue = cache.GetString(cacheKey);
                    // If there is a cached response for this path and the request method is of type 'GET', then break the pipeline and send the cached response.
                    if (!string.IsNullOrEmpty(cachedValue) && (requestMethod == HttpMethod.Get.Method || requestMethod == HttpMethod.Head.Method)) {
                        return Results.Ok(JsonDocument.Parse(cachedValue).RootElement);
                    }

                    var result = await next(efic);

                    var basePath = string.Empty;
                    if (!string.IsNullOrEmpty(cacheKey)) {
                        return result;
                    }
                    // If request method is of type 'GET' then we can cache the response.
                    // Check if we already have a cached value for this cache key
                    // and also that response status code is 200 OK.
                    // and also that response has a value attached to it.
                    if ((requestMethod == HttpMethod.Get.Method || requestMethod == HttpMethod.Head.Method) &&
                        string.IsNullOrEmpty(cachedValue) && 
                        result is IStatusCodeHttpResult httpResult &&
                        httpResult.StatusCode == StatusCodes.Status200OK &&
                        result is IValueHttpResult okResult) {
                        
                        var jsonOptions = eb.ApplicationServices.GetRequiredService<IOptions<JsonOptions>>().Value;
                        cache.SetString(cacheKey, JsonSerializer.Serialize(okResult.Value, jsonOptions.SerializerOptions), new DistributedCacheEntryOptions {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiration)
                        });
                    }
                    // Handle cache invalidation
                    if (requestMethod == HttpMethod.Post.Method || requestMethod == HttpMethod.Put.Method || requestMethod == HttpMethod.Patch.Method || requestMethod == HttpMethod.Delete.Method) {
                        cache.Remove(cacheKey);
                        foreach (var path in dependentPaths) {
                            var dependentKey = $"{basePath}/{path}";
                            var regex = new Regex("{(.*?)}");
                            var match = regex.Match(path);
                            if (!match.Success) {
                                continue;
                            }
                            var routeValue = request.RouteValues.SingleOrDefault(x => $"{{{x.Key}}}" == match.Value);
                            if (routeValue.Value == null) {
                                continue;
                            }
                            dependentKey = dependentKey.Replace(match.Value, routeValue.Value.ToString());
                            var nextMatch = match.NextMatch();
                            var hasNextMatch = nextMatch.Success;
                            while (hasNextMatch) {
                                routeValue = request.RouteValues.SingleOrDefault(x => $"{x.Key}" == nextMatch.Value);
                                dependentKey = dependentKey.Replace(match.Value, routeValue.Value.ToString());
                                nextMatch = nextMatch.NextMatch();
                                hasNextMatch = nextMatch.Success;
                            }
                            cache.Remove(await AddCacheKeyDiscriminatorAsync(efic.HttpContext, varyByClaimType, dependentKey));
                        }
                    }

                    return result;
                });
            });
        });

        return builder;
    }

    /// <summary>
    /// Adds No cache metadata to the current operation <see cref="RouteHandlerBuilder"/>.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns>The builder</returns>
    public static RouteHandlerBuilder NoCache(this RouteHandlerBuilder builder) {
        builder.Add(eb => eb.Metadata.Add(new NoCacheMetadata()));
        return builder;
    }

    private static async Task<string> AddCacheKeyDiscriminatorAsync(HttpContext httpContext, string[] varyByClaimType, string keyMainPart) {
        if (varyByClaimType.Length > 0) {
            var claimValues = varyByClaimType.Select(claim => $"{claim}:{httpContext.User.FindFirstValue(claim)}");
            if (claimValues.Any()) {
                keyMainPart = $"{keyMainPart}|{string.Join('|', claimValues)}";
            }
        }
        var keyExtensionResolver = httpContext.RequestServices.GetService<ICacheResourceFilterKeyExtensionResolver>();
        if (keyExtensionResolver is not null) {
            var keyExtension = await keyExtensionResolver.ResolveCacheKeyExtensionAsync(httpContext, keyMainPart);
            if (keyExtension is not null) {
                keyMainPart = $"{keyMainPart}|{keyExtension}";
            }
        }
        return keyMainPart;
    }

    /// <summary>
    /// NoCache metadata that will override the cache behavior
    /// </summary>
    private sealed record NoCacheMetadata();
}
#endif