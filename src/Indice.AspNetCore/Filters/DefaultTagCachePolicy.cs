#if NET7_0_OR_GREATER
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

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
    /// <param name="cancellation"></param>
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation) {

        var outputCacheSettings = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<OutputCacheSettings>();
        context.EnableOutputCaching = true;
        context.AllowLocking = true;
        context.AllowCacheLookup = AttemptOutputCaching(context, outputCacheSettings);
        context.AllowCacheStorage = AttemptOutputCaching(context, outputCacheSettings);

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


    private string GetCacheTag(HttpContext context) {

        var outputCacheSettings = context.GetEndpoint()?.Metadata.GetMetadata<OutputCacheSettings>();
        var suffix = string.Empty;
        if (outputCacheSettings == null || outputCacheSettings.TagRouteParams?.Any() == false) {
            var routeData = context.GetRouteData();
            suffix = string.Join('|', routeData.Values.Select(x => $"{x.Key}:{x.Value}"));
        } else {
            suffix = string.Join('|', outputCacheSettings.TagRouteParams.Select(name => $"{name}:{context.GetRouteValue(name)}"));
        }

        var cacheTag = (outputCacheSettings.TagPrefix ?? "") + "-" + suffix;
        return cacheTag;
    }

    private bool AttemptOutputCaching(OutputCacheContext context, OutputCacheSettings outputCacheSettings) {
        // Check if the current request fulfills the requirements to be cached

        var request = context.HttpContext.Request;

        // Verify the method
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)) {
            return false;
        }

        // Verify existence of authorization headers
        if (!StringValues.IsNullOrEmpty(request.Headers.Authorization) || request.HttpContext.User?.Identity?.IsAuthenticated == true) {
            return outputCacheSettings.CacheForAuthorisedUsers;
        }
        return true;
    }
}
/// <summary>
/// Cache builder extention
/// </summary>
public static class CacheBuilder
{

    /// <summary>
    /// Adds the provided metadata <paramref name="items"/> to <see cref="EndpointBuilder.Metadata"/> for all builders and adds a CacheouputPolicy
    /// produced by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
    /// <param name="settings">A <see cref="OutputCacheSettings"></see> with cache settings.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/>.</returns>
    public static TBuilder WithCacheOutPutMetadata<TBuilder>(this TBuilder builder, OutputCacheSettings settings) where TBuilder : IEndpointConventionBuilder {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settings);

        builder.Add(b => {
            b.Metadata.Add(settings);
        });
        builder.CacheOutput(policy => {
            policy.AddPolicy<DefaultTagCachePolicy>();
            policy.Expire(settings.Expire);

            if (settings.VaryByRouteValues?.Any() == true) {
                policy.SetVaryByRouteValue(settings.VaryByRouteValues);
            }
        });
        return builder;
    }
}
/// <summary>
/// 
/// </summary>
public class OutputCacheSettings
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
    public List<string> TagRouteParams { get; set; } = new List<string>();
    /// <summary>
    /// The route value names to vary the cached responses by
    /// </summary>
    public string[] VaryByRouteValues { get; set; }
    
    /// <summary>
    /// The query keys to vary the cached responses by 
    /// </summary>
    /// <remarks>
    /// By default all query keys vary the cache entries. However when specific query keys are specified only these are then taken into account.
    /// </remarks>
    public string[] VaryByRouteKeys { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan Expire { get; set; }
}
#endif