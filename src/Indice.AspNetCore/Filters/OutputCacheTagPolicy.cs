#if NET7_0_OR_GREATER
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Filters;

/// <summary>
/// Handle caching for routes that required a more fine grained e.g. caching based on route id
/// </summary>
public class OutputCacheTagPolicy : IOutputCachePolicy
{
    /// <summary>
    /// Updates the <see cref="OutputCacheContext"/> before the cache middleware is invoked.
    /// At that point the cache middleware can still be enabled or disabled for the request.
    /// </summary>
    /// <param name="context">The current request's cache context.</param>
    /// <param name="cancellation"></param>
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation) {

        var outputCacheSettings = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<OutputCacheTagMetadata>();
        context.EnableOutputCaching = true; // default behavior
        context.AllowLocking = true;        // default behavior
        context.AllowCacheLookup = AttemptOutputCaching(context, outputCacheSettings);  // CacheAuthorized
        context.AllowCacheStorage = AttemptOutputCaching(context, outputCacheSettings); // CacheAuthorized

        if (outputCacheSettings != null && !string.IsNullOrEmpty(outputCacheSettings.TagPrefix)) // CacheWithTagPrefix (parameters routeParams)
            context.Tags.Add(GetCacheTag(context.HttpContext, outputCacheSettings));

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


    private static string GetCacheTag(HttpContext context, OutputCacheTagMetadata outputCacheSettings) {
        var suffix = string.Empty;
        if (outputCacheSettings == null || outputCacheSettings.TagRouteParams?.Any() == false) {
            var routeData = context.GetRouteData();
            suffix = string.Join('|', routeData.Values.Select(x => $"{x.Key}:{x.Value}"));
        } else {
            suffix = string.Join('|', outputCacheSettings.TagRouteParams.Select(name => $"{name}:{context.GetRouteValue(name)}"));
        }

        var cacheTag = (outputCacheSettings?.TagPrefix ?? "") + "-" + suffix;
        return cacheTag;
    }

    private static bool AttemptOutputCaching(OutputCacheContext context, OutputCacheTagMetadata outputCacheSettings) {
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
    /// Adds output cache support using the the provided settings <paramref name="settings"/>  for all builders produced by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
    /// <param name="settings">A <see cref="OutputCacheTagMetadata"></see> with cache settings.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/>.</returns>
    public static TBuilder WithOutputCache<TBuilder>(this TBuilder builder, OutputCacheTagMetadata settings) where TBuilder : IEndpointConventionBuilder {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settings);

        builder.Add(b => {
            b.Metadata.Add(settings);
        });
        builder.CacheOutput(policy => {
            policy.AddPolicy<DefaultTagCachePolicy>();

            if (settings.Expire.TotalSeconds > 0) {
                policy.Expire(settings.Expire);
            }

            if (settings.VaryByHeaderNames?.Any() == true) {
                policy.SetVaryByHeader(settings.VaryByHeaderNames);
            }

            if (settings.Tags?.Any() == true) {
                policy.Tag(settings.Tags);
            }

            if (settings.VaryByQueryKeys?.Any() == true) {
                policy.SetVaryByQuery(settings.VaryByQueryKeys);
            }

            if (settings.VaryByHeaderNames?.Any() == true) {
                policy.SetVaryByHeader(settings.VaryByHeaderNames);
            }
        });
        return builder;
    }

    /// <summary>
    /// Marks an endpoint to be cached using the specified policy builder.
    /// </summary>
    /// <param name="policy">An action on <see cref="OutputCachePolicyBuilder"/>.</param>
    /// <param name="excludeDefaultPolicy">Whether to exclude the default policy or not.</param>
    public static TBuilder CacheOutput<TBuilder>(this TBuilder builder, Action<OutputCachePolicyBuilder> policy, bool excludeDefaultPolicy, OutputCacheTagMetadata outputCacheTagMetadata) where TBuilder : IEndpointConventionBuilder {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Add(endpointBuilder => {
            endpointBuilder.Metadata.Add(policy);
            endpointBuilder.Metadata.Add(outputCacheTagMetadata);
        });

        return builder;
    }
}

/// <summary>Options for configuring ASP.NET Core Output caching.</summary>
public class OutputCacheTagMetadata
{
    /// <summary>The tag prefix for the custom key to add to the cached reponse</summary>
    /// <remarks>This key is to be used with <see cref="TagRouteParams"/> </remarks>
    public required string TagPrefix { get; set; }
    /// <summary>The Route params to include in the custom tag</summary>
    public List<string> TagRouteParams { get; set; } = new List<string>();
    /// <summary>The tags to add to the cached reponse.</summary>
    public string[] Tags { get; set; }
    /// <summary>The header names to vary the cached responses by.</summary>
    public string[] VaryByHeaderNames { get; set; }
    /// <summary>Indicates wether the cache should be enforced to authorized users.</summary>
    public bool CacheForAuthorisedUsers { get; set; }
    /// <summary>The route value names to vary the cached responses by</summary>
    public string[] VaryByRouteValues { get; set; }
    /// <summary>The query keys to vary the cached responses by </summary>
    /// <remarks>By default all query keys vary the cache entries. However when specific query keys are specified only these are then taken into account.</remarks>
    public string[] VaryByQueryKeys { get; set; }
    /// <summary>The expiration of the cached reponse</summary>
    public TimeSpan Expire { get; set; }
}
#endif