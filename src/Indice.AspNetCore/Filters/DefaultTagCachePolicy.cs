#if NET7_0_OR_GREATER
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using Microsoft.Extensions.Primitives;

namespace Indice.AspNetCore.Filters;


/// <summary>
/// Handle caching for routes that required a more fine grained e.g. caching based on route id
/// </summary>
public class DefaultTagCachePolicy : IOutputCachePolicy
{
    public bool CacheForAuthorizedUsers { get; set; } = false;
    public DefaultTagCachePolicy(bool cacheForAuthorizedUsers) {
        CacheForAuthorizedUsers = cacheForAuthorizedUsers;
    }
    /// <summary>
    /// Updates the <see cref="OutputCacheContext"/> before the cache middleware is invoked.
    /// At that point the cache middleware can still be enabled or disabled for the request.
    /// </summary>
    /// <param name="context">The current request's cache context.</param>
    /// <param name="cancellation"></param>
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation) {

        //var outputCacheMetadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<OutputCacheMetadata>();
        context.EnableOutputCaching = true;
        context.AllowLocking = true;
        context.AllowCacheLookup = AttemptOutputCaching(context);
        context.AllowCacheStorage = AttemptOutputCaching(context);

        //if (outputCacheMetadata != null)
        //    context.Tags.Add(GetCacheTag(context, outputCacheMetadata));

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Updates the <see cref="OutputCacheContext"/> before the cached response is used.
    /// At that point the freshness of the cached response can be updated.
    /// </summary>
    /// <param name="context">The current request's cache context.</param>
    /// <param name="cancellation"></param>
    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation) => ValueTask.CompletedTask;

    /// <summary>
    /// Updates the <see cref="OutputCacheContext"/> before the response is served and can be cached.
    /// At that point cacheability of the response can be updated.
    /// </summary>
    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation) => ValueTask.CompletedTask;


    private static string GetCacheTag(OutputCacheContext context, OutputCacheMetadata outputCacheMetadata) {

        var endpoint = context.HttpContext.GetEndpoint();
        var routeName = endpoint?.Metadata.GetMetadata<RouteNameMetadata>();

        var suffix = string.Empty;
        if (outputCacheMetadata == null || outputCacheMetadata.VarByRouteParams?.Any() == false) {
            var routeData = context.HttpContext.GetRouteData();
            suffix = string.Join('|', routeData.Values.Select(x => $"{x.Key}:{x.Value}"));
        } else {
            suffix = string.Join('|', context.CacheVaryByRules.RouteValueNames.Select(name => $"{name}:{context.HttpContext.GetRouteValue(name)}"));
        }
        var cacheTag = (routeName?.RouteName ?? "") + "-" + suffix;
        return cacheTag;
    }

    private bool AttemptOutputCaching(OutputCacheContext context) {
        // Check if the current request fulfills the requirements to be cached

        var request = context.HttpContext.Request;

        // Verify the method
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)) {
            return false;
        }

        // Verify existence of authorization headers
        if (!StringValues.IsNullOrEmpty(request.Headers.Authorization) || request.HttpContext.User?.Identity?.IsAuthenticated == true) {
            return CacheForAuthorizedUsers;
        }
        return true;
    }
}

public static class CacheContext
{

    public static string GetCacheTag(this HttpContext context, OutputCacheMetadata outputCacheMetadata) {
        //var outputCacheMetadata = endpoint?.Metadata.GetMetadata<OutputCacheMetadata>();
        var suffix = string.Empty;
        if (outputCacheMetadata == null || outputCacheMetadata.VarByRouteParams?.Any() == false) {
            var routeData = context.GetRouteData();
            suffix = string.Join('|', routeData.Values.Select(x => $"{x.Key}:{x.Value}"));
        } else {
            suffix = string.Join('|', outputCacheMetadata.VarByRouteParams.Select(name => $"{name}:{context.GetRouteValue(name)}"));
        }
        var cacheTag = (outputCacheMetadata.TagPrefix ?? "") + "-" + suffix;
        return cacheTag;
    }
}
/// <summary>
/// 
/// </summary>
public class OutputCacheMetadata
{
    /// <summary>
    /// 
    /// </summary>
    public required string TagPrefix { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public bool CacheForAuthorisedUsers { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public List<string> VarByRouteParams { get; set; }
}
#endif