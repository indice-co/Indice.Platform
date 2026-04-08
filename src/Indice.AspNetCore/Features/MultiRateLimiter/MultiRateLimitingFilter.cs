using Indice.AspNetCore.Configuration;
using Indice.AspNetCore.Features.MultiRateLimiter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// An endpoint filter that enforces multiple rate limiting policies.
/// Can be used with minimal APIs via <see cref="MultiRateLimitingFilterExtensions.WithMultiRateLimiting(RouteHandlerBuilder, string[])"/>.
/// </summary>
public class MultiRateLimitingEndpointFilter : IEndpointFilter
{
    private readonly string[] _policyNames;

    /// <summary>
    /// Creates a new instance of the filter with the specified policy names.
    /// </summary>
    /// <param name="policyNames">The names of the rate limiting policies to apply.</param>
    public MultiRateLimitingEndpointFilter(params string[] policyNames) {
        if (policyNames == null || policyNames.Length == 0) {
            throw new ArgumentException("At least one policy name must be specified.", nameof(policyNames));
        }
        _policyNames = policyNames;
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
        var httpContext = context.HttpContext;
        var rateLimiterService = httpContext.RequestServices.GetRequiredService<IMultiRateLimiterService>();
        var options = httpContext.RequestServices.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<MultiRateLimitingEndpointFilter>>();
        var endpoint = httpContext.GetEndpoint();

        foreach (var policyName in _policyNames) {
            var configurePolicy = rateLimiterService.GetPolicy(policyName);
            if (configurePolicy == null) {
                logger.LogWarning("Policy '{PolicyName}' was not found for endpoint: '{EndpointName}'.", policyName, endpoint?.DisplayName);
                continue;
            }

            var partition = configurePolicy(httpContext);
            var key = $"{policyName}_{httpContext.Request.Path}_{partition.PartitionKey}";
            var limiter = rateLimiterService.GetOrCreateLimiter(key, () => partition.Factory(partition.PartitionKey));

            using var lease = await limiter.AcquireAsync();
            if (!lease.IsAcquired) {
                var rejectionStatusCode = options.RejectionStatusCode ?? StatusCodes.Status429TooManyRequests;
                httpContext.Response.StatusCode = rejectionStatusCode;

                if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
                    httpContext.Items.Add("retry-after", retryAfter.TotalSeconds);
                    httpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                logger.LogInformation("Rate limit reached for policy: {PolicyName} on endpoint: {EndpointName}", policyName, endpoint?.DisplayName);
                return Results.Text("Too many requests. Please try again later.", statusCode: rejectionStatusCode);
            }
        }

        return await next(context);
    }
}

/// <summary>
/// An action filter attribute that enforces multiple rate limiting policies.
/// Can be used with MVC controllers and Razor Pages.
/// </summary>
/// <remarks>
/// Usage for Controllers: [MultiRateLimiting("policy1", "policy2")]
/// Usage for Razor Pages: @attribute [MultiRateLimiting("policy1", "policy2")]
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class MultiRateLimitingAttribute : Attribute, IAsyncActionFilter, IAsyncPageFilter, IOrderedFilter
{
    private readonly string[] _policyNames;

    /// <summary>
    /// Creates a new instance of the attribute with the specified policy names.
    /// </summary>
    /// <param name="policyNames">The names of the rate limiting policies to apply.</param>
    public MultiRateLimitingAttribute(params string[] policyNames) {
        if (policyNames == null || policyNames.Length == 0) {
            throw new ArgumentException("At least one policy name must be specified.", nameof(policyNames));
        }
        _policyNames = policyNames;
    }

    /// <summary>
    /// Gets or sets the order in which the filter executes. Lower values execute first.
    /// Default is 0 (executes early in the pipeline).
    /// </summary>
    public int Order { get; set; } = 0;

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        var result = await TryAcquireRateLimitAsync(context.HttpContext);
        if (result == null) {
            await next();
        } else {
            context.Result = result;
        }
    }

    /// <inheritdoc />
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next) {
        var result = await TryAcquireRateLimitAsync(context.HttpContext);
        if (result == null) {
            await next();
        } else {
            context.Result = result;
        }
    }

    private async Task<IActionResult?> TryAcquireRateLimitAsync(HttpContext httpContext) {
        var rateLimiterService = httpContext.RequestServices.GetRequiredService<IMultiRateLimiterService>();
        var options = httpContext.RequestServices.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<MultiRateLimitingAttribute>>();
        var endpoint = httpContext.GetEndpoint();

        foreach (var policyName in _policyNames) {
            var configurePolicy = rateLimiterService.GetPolicy(policyName);
            if (configurePolicy == null) {
                logger.LogWarning("Policy '{PolicyName}' was not found for endpoint: '{EndpointName}'.", policyName, endpoint?.DisplayName);
                continue;
            }

            var partition = configurePolicy(httpContext);
            var key = $"{policyName}_{httpContext.Request.Path}_{partition.PartitionKey}";
            var limiter = rateLimiterService.GetOrCreateLimiter(key, () => partition.Factory(partition.PartitionKey));

            using var lease = await limiter.AcquireAsync();
            if (!lease.IsAcquired) {
                var rejectionStatusCode = options.RejectionStatusCode ?? StatusCodes.Status429TooManyRequests;

                if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
                    httpContext.Items.Add("retry-after", retryAfter.TotalSeconds);
                    httpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                logger.LogInformation("Rate limit reached for policy: {PolicyName} on endpoint: {EndpointName}", policyName, endpoint?.DisplayName);

                return new ContentResult {
                    Content = "Too many requests. Please try again later.",
                    StatusCode = rejectionStatusCode,
                    ContentType = "text/plain"
                };
            }
        }

        return null; // No rate limit exceeded, continue with the request
    }
}

/// <summary>
/// Extension methods for adding multi-rate limiting to endpoints.
/// </summary>
public static class MultiRateLimitingFilterExtensions
{
    /// <summary>
    /// Adds multiple rate limiting policies to a minimal API endpoint.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="policyNames">The names of the rate limiting policies to apply.</param>
    /// <returns>The route handler builder for chaining.</returns>
    /// <example>
    /// <code>
    /// app.MapGet("/api/resource", () => "Hello")
    ///    .WithMultiRateLimiting("per-user", "per-ip");
    /// </code>
    /// </example>
    public static RouteHandlerBuilder WithMultiRateLimiting(this RouteHandlerBuilder builder, params string[] policyNames) {
        builder.AddEndpointFilter(new MultiRateLimitingEndpointFilter(policyNames));
        return builder;
    }

    /// <summary>
    /// Adds multiple rate limiting policies to a route group.
    /// </summary>
    /// <param name="builder">The route group builder.</param>
    /// <param name="policyNames">The names of the rate limiting policies to apply.</param>
    /// <returns>The route group builder for chaining.</returns>
    /// <example>
    /// <code>
    /// app.MapGroup("/api")
    ///    .WithMultiRateLimiting("per-user", "per-ip")
    ///    .MapGet("/resource", () => "Hello");
    /// </code>
    /// </example>
    public static RouteGroupBuilder WithMultiRateLimiting(this RouteGroupBuilder builder, params string[] policyNames) {
        builder.AddEndpointFilter(new MultiRateLimitingEndpointFilter(policyNames));
        return builder;
    }
}
