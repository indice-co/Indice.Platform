#nullable enable
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;


/// <summary>Endpoint conventions regarding <see cref="OutputCachePolicyBuilder"/> API.</summary>
public static class OutputCacheFilterExtensions
{

    /// <summary>
    /// Removes support for caching authorized request that has been set with the <see cref="SetAuthorized"/> policy.
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <returns>The builder for further configuration</returns>
    public static RouteGroupBuilder IgnoreCacheAuthorized(this RouteGroupBuilder builder) => IgnoreCacheAuthorized<RouteGroupBuilder>(builder);

    /// <summary>
    /// Removes support for caching authorized request that has been set with the <see cref="SetAuthorized"/> policy.
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <returns>The builder for further configuration</returns>
    public static RouteHandlerBuilder IgnoreCacheAuthorized(this RouteHandlerBuilder builder) => IgnoreCacheAuthorized<RouteHandlerBuilder>(builder);

    /// <summary>
    /// Removes support for caching authorized request that has been set with the <see cref="SetAuthorized"/> policy.
    /// </summary>
    /// <typeparam name="TBuilder">The builder type</typeparam>
    /// <param name="builder">The builder to configure</param>
    /// <returns>The builder for further configuration</returns>
    public static TBuilder IgnoreCacheAuthorized<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder {
        builder.WithMetadata(new IgnoreCacheAuthorizedMetadata());
        return builder;
    }

    /// <summary>
    /// Adds tagging metadata used by the output cache in conjunction with the <see cref="SetAutoTag(OutputCachePolicyBuilder)"/> policy .
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <param name="tagPrefix">The tag prefix to use</param>
    /// <param name="routeValueNames">Route parameter names</param>
    /// <param name="claimTypes">Claim type names</param>
    /// <returns>The builder for further configuration</returns>
    public static RouteGroupBuilder WithCacheTag(this RouteGroupBuilder builder, string tagPrefix, string[]? routeValueNames = null, string[]? claimTypes = null) => WithCacheTag<RouteGroupBuilder>(builder, tagPrefix, routeValueNames, claimTypes);

    /// <summary>
    /// Adds tagging metadata used by the output cache in conjunction with the <see cref="SetAutoTag(OutputCachePolicyBuilder)"/> policy .
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <param name="tagPrefix">The tag prefix to use</param>
    /// <param name="routeValueNames">Route parameter names</param>
    /// <param name="claimTypes"></param>
    /// <returns>The builder for further configuration</returns>
    public static RouteHandlerBuilder WithCacheTag(this RouteHandlerBuilder builder, string tagPrefix, string[]? routeValueNames = null, string[]? claimTypes = null) => WithCacheTag<RouteHandlerBuilder>(builder, tagPrefix, routeValueNames, claimTypes);

    /// <summary>
    /// Adds tagging metadata used by the output cache in conjunction with the <see cref="SetAutoTag(OutputCachePolicyBuilder)"/> policy .
    /// </summary>
    /// <typeparam name="TBuilder">The builder type</typeparam>
    /// <param name="builder">The builder to configure</param>
    /// <param name="tagPrefix">The tag prefix to use</param>
    /// <param name="routeValueNames">Route parameter names</param>
    /// <param name="claimTypes">Claim type names</param>
    /// <returns>The builder for further configuration</returns>
    public static TBuilder WithCacheTag<TBuilder>(this TBuilder builder, string tagPrefix, string[]? routeValueNames, string[]? claimTypes) where TBuilder : IEndpointConventionBuilder {
        builder.WithMetadata(new CacheTagMetadata(tagPrefix, routeValueNames, claimTypes));
        return builder;
    }

    /// <summary>
    /// Adds a policy that varies the cache tag using the <strong>WithCacheTag()</strong> metadata.
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    public static OutputCachePolicyBuilder SetAutoTag(this OutputCachePolicyBuilder builder) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPolicy<SetCacheAutoTagPolicy>();
    }

    /// <summary>
    /// Adds a policy that varies the cache key using the <strong>CacheAuthorized()</strong> metadata.
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <param name="keyPrefix">The value to vary the cache key by. Optional will also set the Cache key prefix policy</param>
    /// <remarks><strong>Be careful!!</strong> This could make endpoints that otherwize would not cache between different user context now to return the same value.</remarks>
    public static OutputCachePolicyBuilder SetAuthorized(this OutputCachePolicyBuilder builder, Func<HttpContext, string>? keyPrefix = null) {
        ArgumentNullException.ThrowIfNull(builder);
        if (keyPrefix is not null) {
            builder.SetCacheKeyPrefix(keyPrefix);
        }
        return builder.AddPolicy<SetCacheAuthorizedPolicy>();
    }

    /// <summary>Adds the ability to invalidate cache for responses.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <param name="tagName">A tag name that must be invalidated.</param>
    /// <returns>The builder.</returns>
    public static TBuilder InvalidateCacheTag<TBuilder>(this TBuilder builder, string tagName) where TBuilder : IEndpointConventionBuilder {
        ArgumentException.ThrowIfNullOrEmpty(tagName);
        InvalidateCacheTag(builder, getTagName: (httpContext) => ValueTask.FromResult(tagName));
        return builder;
    }

    /// <summary>Adds the ability to invalidate cache for responses.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <param name="tagPrefix">The tag prefix to use</param>
    /// <param name="routeValueNames">Route parameter names</param>
    /// <returns>The builder.</returns>
    public static TBuilder InvalidateCacheTag<TBuilder>(this TBuilder builder, string tagPrefix, string[] routeValueNames) where TBuilder : IEndpointConventionBuilder =>
        InvalidateCacheTag(builder, (httpContext) => ValueTask.FromResult(CacheTagMetadata.CreateTag(httpContext, tagPrefix, routeValueNames, [])));

    /// <summary>Adds the ability to invalidate cache for responses.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <param name="tagPrefix">The tag prefix to use</param>
    /// <param name="routeValueNames">Route parameter names</param>
    /// <param name="claimTypes">Claim type names</param>
    /// <returns>The builder.</returns>
    public static TBuilder InvalidateCacheTag<TBuilder>(this TBuilder builder, string tagPrefix, string[] routeValueNames, string[] claimTypes) where TBuilder : IEndpointConventionBuilder =>
        InvalidateCacheTag(builder, (httpContext) => ValueTask.FromResult(CacheTagMetadata.CreateTag(httpContext, tagPrefix, routeValueNames, claimTypes)));

    /// <summary>Adds the ability to invalidate cache for responses.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <param name="tagPrefix">The tag prefix to use</param>
    /// <param name="getTagData">The method that will return the tag data to use to create the tagName to invalidate</param>
    /// <returns>The builder.</returns>
    public static TBuilder InvalidateCacheTag<TBuilder>(this TBuilder builder, string tagPrefix, Func<HttpContext, IEnumerable<KeyValuePair<string, object?>>> getTagData) where TBuilder : IEndpointConventionBuilder =>
        InvalidateCacheTag(builder, (httpContext) => ValueTask.FromResult(CacheTagMetadata.CreateTag(tagPrefix, getTagData(httpContext))));


    /// <summary>Adds the ability to invalidate cache for responses.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <param name="getTagName">The method that will return the tag name to invalidate</param>
    /// <returns>The builder.</returns>
    public static TBuilder InvalidateCacheTag<TBuilder>(this TBuilder builder, Func<HttpContext, ValueTask<string>> getTagName) where TBuilder : IEndpointConventionBuilder {
        builder.Add(endpointBuilder => {
            endpointBuilder.FilterFactories.Add((context, next) => {
                return new EndpointFilterDelegate(async (invocationContext) => {
                    var requestMethod = invocationContext.HttpContext.Request.Method;
                    var outputCacheStore = invocationContext.HttpContext.RequestServices.GetRequiredService<IOutputCacheStore>();

                    var result = await next(invocationContext);

                    var isSuccessStatusCode = invocationContext.HttpContext.Response.StatusCode >= 200 && invocationContext.HttpContext.Response.StatusCode < 300;
                    var isStateChangingMethod = requestMethod == HttpMethod.Post.Method || requestMethod == HttpMethod.Put.Method || requestMethod == HttpMethod.Patch.Method || requestMethod == HttpMethod.Delete.Method;
                    if (isSuccessStatusCode && isStateChangingMethod) {

                        // Handle cache invalidation.
                        var tagName = await getTagName(invocationContext.HttpContext);
                        await outputCacheStore.EvictByTagAsync(tagName, default);
                    }

                    return result;
                });
            });
        });
        return builder;
    }
}

/// <summary>
/// A policy that sets the allow cache always when regardless of the authorization context.
/// </summary>
/// <remarks><strong>Be careful!!</strong> This could make endpoints that otherwize would not cache between different user context now to return the same value.</remarks>
internal sealed class SetCacheAuthorizedPolicy : IOutputCachePolicy
{
    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation) {
        var metadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IgnoreCacheAuthorizedMetadata>();
        if (metadata is null) {
            SetAllowCache(context);
        }
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation) => ValueTask.CompletedTask;
    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation) => ValueTask.CompletedTask;


    internal void SetAllowCache(OutputCacheContext cacheContext) {
        var request = cacheContext.HttpContext.Request;
        var shoudCache = HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);

        if (!shoudCache) {
            return;
        }
        cacheContext.AllowCacheLookup = cacheContext.AllowCacheStorage = true;
    }
}

/// <summary>
/// A policy that sets the cache tag using the specified value.
/// </summary>
internal sealed class SetCacheAutoTagPolicy : IOutputCachePolicy
{
    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation) {
        var metadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<CacheTagMetadata>();
        metadata?.SetTag(context);
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation) => ValueTask.CompletedTask;
    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation) => ValueTask.CompletedTask;
}

internal sealed record IgnoreCacheAuthorizedMetadata();

internal sealed record CacheTagMetadata(string TagPrefix, string[]? RouteValueNames, string[]? ClaimTypes)
{
    internal const char TAG_PART_DELIMITER = '|';
    internal string SetTag(OutputCacheContext cacheContext) {
        var tag = CreateTag(cacheContext.HttpContext, TagPrefix, RouteValueNames ?? [], ClaimTypes ?? []);
        cacheContext.Tags.Add(tag);
        return tag;
    }


    internal static string CreateTag(HttpContext httpContext, string tagPrefix, string[] RouteValueNames, string[] claimTypes) {
        var routeParams = RouteValueNames.Select(name => new KeyValuePair<string, object?>(name, httpContext.GetRouteValue(name)));
        var claimParams = claimTypes.Select(name => new KeyValuePair<string, object?>(name, httpContext.User.FindFirstValue(name)));
        return CreateTag(tagPrefix, [.. routeParams, .. claimParams]);
    }

    internal static string CreateTag(string tagPrefix, IEnumerable<KeyValuePair<string, object?>> keyValuePairs) =>
        tagPrefix + TAG_PART_DELIMITER + string.Join(TAG_PART_DELIMITER, keyValuePairs.Select(x => $"{x.Key}:{x.Value}"));
}
#nullable disable