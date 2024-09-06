using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Indice.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Http.Filters;

/// <summary>Endpoint memory caching extensions.</summary>
public static class CacheResourceFilterExtensions
{
#if NET7_0_OR_GREATER
    /// <summary>Adds the ability to cache the responses.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <param name="expiration">The absolute expiration in minutes of the cache item, expressed as an <see cref="int"/>. Defaults to 60 minutes.</param>
    /// <param name="varyByClaimType">The claim to use which value is included in the cache key.</param>
    /// <returns>The builder.</returns>
    public static TBuilder CacheOutputMemory<TBuilder>(this TBuilder builder, int expiration = 60, string[] varyByClaimType = null)
        where TBuilder : IEndpointConventionBuilder {
        builder.Add(endpointBuilder => {
            var cacheResourceFilterOptions = endpointBuilder.ApplicationServices.GetService<IOptions<CacheResourceFilterOptions>>()?.Value ?? new CacheResourceFilterOptions();
            if (cacheResourceFilterOptions?.DisableCache == true) {
                return;
            }
            varyByClaimType ??= Array.Empty<string>();
            endpointBuilder.FilterFactories.Add((context, next) => {
                var noCache = endpointBuilder.Metadata.OfType<NoCacheMetadata>().Any();
                if (noCache) {
                    return next;
                }
                return new EndpointFilterDelegate(async invocationContext => {
                    var request = invocationContext.HttpContext.Request;
                    var requestMethod = request.Method;
                    var cache = invocationContext.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
                    var cacheKey = $"{request.Path}{(request.QueryString.HasValue ? request.QueryString.Value : string.Empty)}";
                    cacheKey = await AddCacheKeyDiscriminatorAsync(invocationContext.HttpContext, varyByClaimType, cacheKey);
                    var cachedValue = cache.GetString(cacheKey);
                    // If there is a cached response for this path and the request method is of type 'GET', then break the pipeline and send the cached response.
                    if (!string.IsNullOrEmpty(cachedValue) && (requestMethod == HttpMethod.Get.Method || requestMethod == HttpMethod.Head.Method)) {
                        return Results.Ok(JsonDocument.Parse(cachedValue).RootElement);
                    }
                    var result = await next(invocationContext);
                    // Check if the cache key has been properly set, which is crucial for interacting with the cache.
                    if (string.IsNullOrEmpty(cacheKey)) {
                        return result;
                    }
                    // If request method is of type 'GET' then we can cache the response. Check if we already have a cached value for this cache key and also that response status code is 200 OK.
                    // Also that response has a value attached to it.
                    if ((requestMethod == HttpMethod.Get.Method || requestMethod == HttpMethod.Head.Method) &&
                        string.IsNullOrEmpty(cachedValue)) {
                        object value;
                        switch (result) {
                            case INestedHttpResult nestedHttpResult when nestedHttpResult.Result is IStatusCodeHttpResult httpResult && httpResult.StatusCode == StatusCodes.Status200OK && httpResult is IValueHttpResult okResult:
                                value = okResult.Value;
                                break;
                            case IStatusCodeHttpResult httpResult2 when httpResult2.StatusCode == StatusCodes.Status200OK && result is IValueHttpResult okResult2:
                                value = okResult2.Value;
                                break;
                            default: return result;
                        };
                        var jsonOptions = endpointBuilder.ApplicationServices.GetRequiredService<IOptions<JsonOptions>>().Value;
                        cache.SetString(cacheKey, JsonSerializer.Serialize(value, jsonOptions.SerializerOptions), new DistributedCacheEntryOptions {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiration)
                        });
                    }
                    return result;
                });
            });
        });
        return builder;
    }

    /// <summary>Adds the ability to invalidate cache for responses.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <param name="dependentRoute">Parent RouteName of the current method that must be invalidated. Path template variables must match by name.</param>
    /// <returns>The builder.</returns>
    public static TBuilder InvalidateCache<TBuilder>(this TBuilder builder, string dependentRoute) where TBuilder : IEndpointConventionBuilder {
        ArgumentException.ThrowIfNullOrEmpty(dependentRoute, nameof(dependentRoute));
        InvalidateCache(builder, dependentRoutes: [dependentRoute], varyByClaimType: null);
        return builder;
    }

    /// <summary>Adds the ability to invalidate cache for responses.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <param name="dependentRoute">Parent RouteName of the current method that must be invalidated. Path template variables must match by name.</param>
    /// <param name="varyByClaimType">The claim to use which value is included in the cache key.</param>
    /// <returns>The builder.</returns>
    public static TBuilder InvalidateCache<TBuilder>(this TBuilder builder, string dependentRoute, string varyByClaimType) where TBuilder : IEndpointConventionBuilder {
        ArgumentException.ThrowIfNullOrEmpty(dependentRoute, nameof(dependentRoute));
        ArgumentException.ThrowIfNullOrEmpty(varyByClaimType, nameof(varyByClaimType));
        InvalidateCache(builder, dependentRoutes: [dependentRoute], varyByClaimType: [varyByClaimType]);
        return builder;
    }

    /// <summary>Adds the ability to invalidate cache for responses.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <param name="dependentRoutes">Parent paths of the current method that must be invalidated. Path template variables must match by name.</param>
    /// <param name="varyByClaimType">The claim to use which value is included in the cache key.</param>
    /// <returns>The builder.</returns>
    public static TBuilder InvalidateCache<TBuilder>(this TBuilder builder, string[] dependentRoutes, string[] varyByClaimType = null) where TBuilder : IEndpointConventionBuilder {
        ArgumentNullException.ThrowIfNull(dependentRoutes, nameof(dependentRoutes));
        builder.Add(endpointBuilder => {
            var cacheResourceFilterOptions = endpointBuilder.ApplicationServices.GetService<IOptions<CacheResourceFilterOptions>>()?.Value ?? new CacheResourceFilterOptions();
            if (cacheResourceFilterOptions?.DisableCache == true) {
                return;
            }
            dependentRoutes ??= [];
            varyByClaimType ??= [];
            endpointBuilder.FilterFactories.Add((context, next) => {
                return new EndpointFilterDelegate(async invocationContext => {
                    var request = invocationContext.HttpContext.Request;
                    var requestMethod = request.Method;
                    var cache = invocationContext.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
                    var cacheKey = $"{request.Path}{(request.QueryString.HasValue ? request.QueryString.Value : string.Empty)}";
                    cacheKey = await AddCacheKeyDiscriminatorAsync(invocationContext.HttpContext, varyByClaimType, cacheKey);
                    var result = await next(invocationContext);
                    // Check if the cache key has been properly set, which is crucial for interacting with the cache.
                    if (string.IsNullOrEmpty(cacheKey)) {
                        return result;
                    }
                    // Handle cache invalidation.
                    if (requestMethod == HttpMethod.Post.Method || requestMethod == HttpMethod.Put.Method || requestMethod == HttpMethod.Patch.Method || requestMethod == HttpMethod.Delete.Method) {
                        cache.Remove(cacheKey);
                        var linkGenerator = invocationContext.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();
                        foreach (var routeName in dependentRoutes) {
                            var url = linkGenerator.GetPathByRouteValues(
                                invocationContext.HttpContext,
                                routeName,
                                invocationContext.HttpContext.GetRouteData().Values,
                                fragment: FragmentString.Empty
                            );
                            if (url is null) {
                                continue;
                            }
                            cache.Remove(await AddCacheKeyDiscriminatorAsync(invocationContext.HttpContext, varyByClaimType, url));
                        }
                    }

                    return result;
                });
            });
        });
        return builder;
    }


    /// <summary>Adds No cache metadata to the current operation <see cref="RouteHandlerBuilder"/>.</summary>
    /// <param name="builder">Builds conventions that will be used for customization of MapAction <see cref="EndpointBuilder"/> instances.</param>
    /// <returns>The builder.</returns>
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

#endif
    /// <summary>NoCache metadata that will override the cache behavior.</summary>
    private sealed record NoCacheMetadata();
}