#if NET7_0_OR_GREATER
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Filters;


/// <summary>
/// Handle caching for routes that required a more fine grained e.g. caching based on route id
/// </summary>
public class DefaultTagCachePolicy : IOutputCachePolicy
{

    /// <summary>
    /// Updates the <see cref="OutputCacheContext"/> before the cache middleware is invoked.
    /// At that point the cache middleware can still be enabled or disabled for the request.
    /// </summary>
    /// <param name="context">The current request's cache context.</param>
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation) {
        var attemptOutputCaching = AttemptOutputCaching(context);
        context.EnableOutputCaching = true;
        context.AllowCacheLookup = attemptOutputCaching;
        context.AllowCacheStorage = attemptOutputCaching;
        context.AllowLocking = true;

        // Vary by any query by default
        context.CacheVaryByRules.QueryKeys = "*";
        return ValueTask.CompletedTask;
    }
    /// <summary>
    /// Updates the <see cref="OutputCacheContext"/> before the cached response is used.
    /// At that point the freshness of the cached response can be updated.
    /// </summary>
    /// <param name="context">The current request's cache context.</param>
    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation) => ValueTask.CompletedTask;

    /// <summary>
    /// Updates the <see cref="OutputCacheContext"/> before the response is served and can be cached.
    /// At that point cacheability of the response can be updated.
    /// </summary>
    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation) {
        var endpoint = context.HttpContext.GetEndpoint();
        var routeName = endpoint?.Metadata.GetMetadata<RouteNameMetadata>();

        var suffix = string.Empty;
        if (context.CacheVaryByRules.RouteValueNames == "*" || string.IsNullOrEmpty(context.CacheVaryByRules.RouteValueNames)) {
            var routeData = context.HttpContext.GetRouteData();
            suffix = string.Join('|', routeData.Values.Select(x => $"{x.Key}:{x.Value}"));
        } else {
            suffix = string.Join('|', context.CacheVaryByRules.RouteValueNames.Select(name => $"{name}:{context.HttpContext.GetRouteValue(name)}"));
        }
        var cacheTag = (routeName?.RouteName ?? "") + "-" + suffix;
        
        context.Tags.Add(cacheTag);
        return ValueTask.CompletedTask;
    }

    private static bool AttemptOutputCaching(OutputCacheContext context) {
        // Check if the current request fulfills the requirements to be cached

        var request = context.HttpContext.Request;

        // Verify the method
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)) {
            return false;
        }

        // Verify existence of authorization headers
        //if (!StringValues.IsNullOrEmpty(request.Headers.Authorization) || request.HttpContext.User?.Identity?.IsAuthenticated == true)
        //{
        //    return false;
        //}
        return true;
    }
}
#endif