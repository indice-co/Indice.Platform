using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Indice.AspNetCore.Configuration;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions related to RateLimiter</summary>
public static class RateLimiterExtensions
{
    /// <summary>Registers the rate limiter services.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configureOptions">An action to configure the rate limiter options.</param>
    /// <param name="configurationSectionName">The configuration section name. Optional.</param>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services,
        IConfiguration configuration, Action<RateLimiterOptions> configureOptions, string? configurationSectionName = null) {
        var sectionName = string.IsNullOrEmpty(configurationSectionName) ? RateLimiterOptions.SectionName : configurationSectionName;

        var rateLimiterOptions = new RateLimiterOptions();
        configuration.GetSection(sectionName).Bind(rateLimiterOptions);
        configureOptions.Invoke(rateLimiterOptions);

        services.AddRateLimiter(options => {
            foreach (var endpoint in rateLimiterOptions.AllRateLimiterPolicies) {
                var endpointOptions = rateLimiterOptions.Rules.FirstOrDefault(rule => rule.Endpoint == endpoint) ?? rateLimiterOptions.GetPolicySettings(endpoint);
                options.AddPolicy(endpoint, context => {
                    if (!endpointOptions.CanLimitHttpMethod(context.Request.Method)) {
                        return RateLimitPartition.GetNoLimiter("NoRateLimiting");
                    }
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetPartitionKey(context, rateLimiterOptions.UserIdentifierClaimType, endpointOptions.PartitionByProperty),
                        factory: _ => new FixedWindowRateLimiterOptions {
                            PermitLimit = endpointOptions.PermitLimit.GetValueOrDefault(),
                            QueueLimit = endpointOptions.QueueLimit.GetValueOrDefault(),
                            QueueProcessingOrder = endpointOptions.QueueProcessingOrder.GetValueOrDefault(),
                            Window = endpointOptions.Window.GetValueOrDefault()
                        });
                });
            }
            options.RejectionStatusCode = rateLimiterOptions.RejectionStatusCode.GetValueOrDefault();
            options.OnRejected = (context, cancellationToken) => {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
                    context.HttpContext.Items.Add("retry-after", retryAfter.TotalSeconds);
                }
                return ValueTask.CompletedTask;
            };
        });
        return services;
    }
    // Helper method to determine the partition key based on user claims, request body, or fallback to IP/Host
    private static string GetPartitionKey(HttpContext httpContext, string userIdentifierClaimType, string? partitionByProperty) {
        string? partitionKey = null;
        // Try to get from user claims first
        partitionKey = httpContext.User.FindFirstValue(userIdentifierClaimType);
        // If not authenticated, try to get the partition property from request
        if (!string.IsNullOrEmpty(partitionKey)) {
            return partitionKey;
        }
        if (string.IsNullOrEmpty(partitionByProperty)) {
            return httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString();
        }
        // Enable buffering so the body can be read multiple times
        httpContext.Request.EnableBuffering();
        try {
            // Handle form data
            if (httpContext.Request.HasFormContentType) {
                if (httpContext.Request.Form.TryGetValue(partitionByProperty, out var propertyValue)) {
                    partitionKey = NormalizePartitionKey(propertyValue.ToString());
                }
            }
            // Handle JSON content
            else if (httpContext.Request.ContentType != null && httpContext.Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)) {
                var requestBody = httpContext.Request.Body;
                long? originalPosition = null;
                if (requestBody.CanSeek) {
                    originalPosition = requestBody.Position;
                    requestBody.Position = 0;
                }
                try {
                    // Limit body size to prevent reading huge payloads
                    const int MaxBodySize = 4096;

                    // Use ReadAsync with blocking (required because partition resolver is synchronous)
                    var buffer = new byte[MaxBodySize];
                    var bytesRead = requestBody.ReadAsync(buffer, 0, MaxBodySize).GetAwaiter().GetResult();

                    if (bytesRead > 0) {
                        var body = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        try {
                            using var jsonDoc = System.Text.Json.JsonDocument.Parse(body);
                            if (jsonDoc.RootElement.TryGetProperty(partitionByProperty, out var propertyElement)) {
                                partitionKey = NormalizePartitionKey(propertyElement.GetString());
                            }
                        } catch (System.Text.Json.JsonException) {
                            // Invalid JSON, fall back to IP
                        }
                    }
                } catch {
                    // fall back to IP or Host
                } finally {
                    if (requestBody.CanSeek && originalPosition.HasValue) {
                        requestBody.Position = originalPosition.Value;
                    }
                }
            }
        } catch {
            // fall back to IP or Host
        }
        // Fallback to IP or Host
        partitionKey ??= httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString();
        return partitionKey;
    }

    /// <summary>
    /// Normalizes a raw partition key value by trimming, checking for emptiness, and bounding its length.
    /// Long values are replaced with a fixed-length SHA-256 hash to keep the key size bounded.
    /// </summary>
    /// <param name="rawKey">The raw key value extracted from the request or user claims.</param>
    /// <returns>A normalized, bounded key suitable for use as a partition key, or <c>null</c> if not usable.</returns>
    private static string? NormalizePartitionKey(string? rawKey) {
        if (string.IsNullOrWhiteSpace(rawKey)) {
            return null;
        }
        var trimmed = rawKey.Trim();
        if (trimmed.Length == 0) {
            return null;
        }
        // If the key is already reasonably small, use it as-is.
        const int MaxKeyLength = 128;
        if (trimmed.Length <= MaxKeyLength) {
            return trimmed;
        }
        // For very long keys, use a fixed-length hash to bound the size.
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(trimmed);
        var hash = sha256.ComputeHash(bytes);
        var builder = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}