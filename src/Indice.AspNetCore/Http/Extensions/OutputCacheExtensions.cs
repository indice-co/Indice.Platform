#if NET7_0_OR_GREATER
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;


/// <summary>Endpoint conventions regarding <see cref="OutputCachePolicyBuilder"/> API.</summary>
public static class OutputCacheFilterExtensions
{

    /// <summary>
    /// Adds support for caching authorized request as well as public ones (default behavior). It adds metadata used by the output cache in conjunction with the <see cref="SetAuthorized"/> policy.
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <returns>The builder for further configuration</returns>
    /// <remarks><strong>Be careful!!</strong> This could make endpoints that otherwize would not cache between different user context now to return the same value.</remarks>
    public static RouteGroupBuilder CacheAuthorized(this RouteGroupBuilder builder) => CacheAuthorized<RouteGroupBuilder>(builder);

    /// <summary>
    /// Adds support for caching authorized request as well as public ones (default behavior). It adds metadata used by the output cache in conjunction with the <see cref="SetAuthorized"/> policy.
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <returns>The builder for further configuration</returns>
    /// <remarks><strong>Be careful!!</strong> This could make endpoints that otherwize would not cache between different user context now to return the same value.</remarks>
    public static RouteHandlerBuilder CacheAuthorized(this RouteHandlerBuilder builder) => CacheAuthorized<RouteHandlerBuilder>(builder);

    /// <summary>
    /// Adds support for caching authorized request as well as public ones (default behavior). It adds metadata used by the output cache in conjunction with the <see cref="SetAuthorized"/> policy.
    /// </summary>
    /// <typeparam name="TBuilder">The builder type</typeparam>
    /// <param name="builder">The builder to configure</param>
    /// <returns>The builder for further configuration</returns>
    /// <remarks><strong>Be careful!!</strong> This could make endpoints that otherwize would not cache between different user context now to return the same value.</remarks>
    public static TBuilder CacheAuthorized<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder {
        builder.WithMetadata(new CacheAuthorizedMetadata());
        return builder;
    }

    /// <summary>
    /// Adds tagging metadata used by the output cache in conjunction with the <see cref="SetTagPrefix"/> policy .
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <param name="tagPrefix">The tag prefix to use</param>
    /// <param name="routeValueNames">Route parameter names</param>
    /// <returns>The builder for further configuration</returns>
    public static RouteGroupBuilder WithCacheTag(this RouteGroupBuilder builder, string tagPrefix, params string[] routeValueNames) => WithCacheTag<RouteGroupBuilder>(builder, tagPrefix, routeValueNames);

    /// <summary>
    /// Adds tagging metadata used by the output cache in conjunction with the <see cref="SetTagPrefix"/> policy .
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <param name="tagPrefix">The tag prefix to use</param>
    /// <param name="routeValueNames">Route parameter names</param>
    /// <returns>The builder for further configuration</returns>
    public static RouteHandlerBuilder WithCacheTag(this RouteHandlerBuilder builder, string tagPrefix, params string[] routeValueNames) => WithCacheTag<RouteHandlerBuilder>(builder, tagPrefix, routeValueNames);

    /// <summary>
    /// Adds tagging metadata used by the output cache in conjunction with the <see cref="SetTagPrefix"/> policy .
    /// </summary>
    /// <typeparam name="TBuilder">The builder type</typeparam>
    /// <param name="builder">The builder to configure</param>
    /// <param name="tagPrefix">The tag prefix to use</param>
    /// <param name="routeValueNames">Route parameter names</param>
    /// <returns>The builder for further configuration</returns>
    public static TBuilder WithCacheTag<TBuilder>(this TBuilder builder, string tagPrefix, params string[] routeValueNames) where TBuilder : IEndpointConventionBuilder {
        builder.WithMetadata(new CacheTagPrefixMetadata(tagPrefix, routeValueNames));
        return builder;
    }

    /// <summary>
    /// Adds a policy that varies the cache tag using the <strong>WithCacheTag()</strong> metadata.
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    public static OutputCachePolicyBuilder SetTagPrefix(this OutputCachePolicyBuilder builder) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPolicy<SetCacheTagPrefixPolicy>();
    }

    /// <summary>
    /// Adds a policy that varies the cache key using the <strong>CacheAuthorized()</strong> metadata.
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <remarks><strong>Be careful!!</strong> This could make endpoints that otherwize would not cache between different user context now to return the same value.</remarks>
    public static OutputCachePolicyBuilder SetAuthorized(this OutputCachePolicyBuilder builder) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPolicy<SetCacheAuthorizedPolicy>();
    }
}

/// <summary>
/// A policy that sets the allow cache always when regardless of the authorization context.
/// </summary>
/// <remarks><strong>Be careful!!</strong> This could make endpoints that otherwize would not cache between different user context now to return the same value.</remarks>
internal sealed class SetCacheAuthorizedPolicy : IOutputCachePolicy
{
    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken) {
        var metadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<CacheAuthorizedMetadata>();
        metadata?.SetAllowCache(context);
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

/// <summary>
/// A policy that sets the cache tag using the specified value.
/// </summary>
internal sealed class SetCacheTagPrefixPolicy : IOutputCachePolicy
{
    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken) {
        var metadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<CacheTagPrefixMetadata>();
        metadata?.SetTag(context);
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

internal record CacheAuthorizedMetadata {
    internal void SetAllowCache(OutputCacheContext cacheContext) {
        var request = cacheContext.HttpContext.Request;
        var shoudCache = HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);

        if (!shoudCache) {
            return;
        }
        cacheContext.AllowCacheLookup = cacheContext.AllowCacheStorage = true;
    }
}

internal record CacheTagPrefixMetadata(string TagPrefix, params string[] RouteValueNames)
{
    internal const char TAG_PART_DELIMITER = '|';
    internal string SetTag(OutputCacheContext cacheContext) {
        var tag = TagPrefix + TAG_PART_DELIMITER;
        if (RouteValueNames?.Length > 0) {
            tag += string.Join(TAG_PART_DELIMITER, RouteValueNames.Select(name => $"{name}:{cacheContext.HttpContext.GetRouteValue(name)}"));
        } else {
            var routeData = cacheContext.HttpContext.GetRouteData();
            tag += string.Join(TAG_PART_DELIMITER, routeData.Values.Select(x => $"{x.Key}:{x.Value}"));
        }
        cacheContext.Tags.Add(tag);
        return tag;
    }
}
#endif