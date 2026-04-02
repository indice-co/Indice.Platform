using Indice.AspNetCore.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace Indice.AspNetCore.Features.MultiRateLimiter;

/// <summary>
/// Middleware that enforces multiple rate limiting policies on endpoints.
/// </summary>
public class MultiRateLimitingMiddleware(
        RequestDelegate next,
        ILogger<MultiRateLimitingMiddleware> logger,
        IOptions<RateLimiterOptions> options,
        IMultiRateLimiterService rateLimiterService)
{
    private readonly RateLimiterOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context) {
        var endpoint = context.GetEndpoint();
        var metadata = endpoint?.Metadata.GetMetadata<MultiRateLimitingMetadata>();

        // If no rate limiting metadata, skip this middleware
        if (metadata == null) {
            await next(context);
            return;
        }

        foreach (var policyName in metadata.PolicyNames) {
            var configurePolicy = rateLimiterService.GetPolicy(policyName);
            if (configurePolicy == null) {
                logger.LogWarning("Policy '{PolicyName}' was not found for endpoint: '{EndpointName}'.", policyName, endpoint?.DisplayName);
                continue;
            }

            var partition = configurePolicy(context);

            var key = $"{policyName}_{context.Request.Path}_{partition.PartitionKey}";
            var limiter = rateLimiterService.GetOrCreateLimiter(key,
                () => partition.Factory(partition.PartitionKey));

            using var lease = await limiter.AcquireAsync();
            if (!lease.IsAcquired) {
                var rejectionStatusCode = _options.RejectionStatusCode ?? StatusCodes.Status429TooManyRequests;
                context.Response.StatusCode = rejectionStatusCode;

                if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
                    context.Items.Add("retry-after", retryAfter.TotalSeconds);
                }

                logger.LogInformation("Rate limit reached for policy: {PolicyName} on endpoint: {EndpointName}", policyName, endpoint?.DisplayName);
                await context.Response.WriteAsync("Too many requests. Please try again later.");
                return;
            }
        }
        await next(context);
    }
}

/// <summary>
/// Metadata that stores the rate limiting policy names for an endpoint.
/// </summary>
public sealed class MultiRateLimitingMetadata
{
    /// <summary>
    /// The names of the rate limiting policies to apply.
    /// </summary>
    public string[] PolicyNames { get; }

    /// <summary>
    /// Creates metadata with the specified policy names.
    /// </summary>
    /// <param name="policyNames">The names of the rate limiting policies to apply.</param>
    public MultiRateLimitingMetadata(params string[] policyNames) {
        if (policyNames == null || policyNames.Length == 0) {
            throw new ArgumentException("At least one policy name must be specified.", nameof(policyNames));
        }
        PolicyNames = policyNames;
    }
}
