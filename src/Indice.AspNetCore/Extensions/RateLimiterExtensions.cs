using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using System.Globalization;
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
                var defaultPolicies = rateLimiterOptions.GetPolicySettings(endpoint);
                var endpointOptions = rateLimiterOptions.Rules.FirstOrDefault(rule => rule.Endpoint == endpoint) ?? defaultPolicies.First();
                options.AddPolicy(endpoint, context => {
                    if (!endpointOptions.CanLimitHttpMethod(context.Request.Method)) {
                        return RateLimitPartition.GetNoLimiter("NoRateLimiting");
                    }
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetPartitionKey(context, rateLimiterOptions.UserIdentifierClaimType, endpointOptions),
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
    // Helper method to determine the partition key based on strategy, user claims, request body, or fallback to IP/Host
    private static string GetPartitionKey(HttpContext httpContext, string userIdentifierClaimType, RateLimiterEndpointRule rule) {
        var strategy = rule.PartitionStrategy;

        switch (strategy) {
            case RateLimiterPartitionStrategy.IpAddress:
                return httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString();
            case RateLimiterPartitionStrategy.RequestProperty:
                if (string.IsNullOrEmpty(rule.PartitionByProperty)) {
                    return httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString();
                }
                return ExtractPropertyFromRequest(httpContext, rule.PartitionByProperty)
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? httpContext.Request.Headers.Host.ToString();

            case RateLimiterPartitionStrategy.User:
            case RateLimiterPartitionStrategy.Auto:
            default:
                return httpContext.User.FindFirstValue(userIdentifierClaimType)
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? httpContext.Request.Headers.Host.ToString();
        }
    }

    private static string? ExtractPropertyFromRequest(HttpContext httpContext, string partitionByProperty) {
        httpContext.Request.EnableBuffering();
        try {
            // Handle form data
            if (httpContext.Request.HasFormContentType) {
                if (httpContext.Request.Form.TryGetValue(partitionByProperty, out var propertyValue)) {
                    return NormalizePartitionKey(propertyValue.ToString());
                }
            }
            // Handle JSON content
            else if (httpContext.Request.ContentType != null && httpContext.Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)) {
                var requestBody = httpContext.Request.Body;
                requestBody.Position = 0;
                try {
                    var property = FindPropertyValue(requestBody, partitionByProperty).GetAwaiter().GetResult();
                    return NormalizePartitionKey(property);
                } finally {
                    requestBody.Position = 0;
                }
            }
        } catch {
            // fall back
        }
        return null;
    }

    /// <summary>
    /// Optimized version using stackalloc for small buffers and avoiding allocations.
    /// </summary>
    public static async Task<string?> FindPropertyValue(Stream jsonStream, string property) {
        string[] path = property.Split('.');
        const int chunkSize = 4096;
        byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(chunkSize);

        try {
            int bytesInBuffer = 0;
            int bytesRead;
            var state = new JsonReaderState();
            int matchIndex = 0;
            int currentPathDepth = 0;

            while ((bytesRead = await jsonStream.ReadAsync(buffer.AsMemory(bytesInBuffer, buffer.Length - bytesInBuffer))) > 0) {
                bytesInBuffer += bytesRead;
                bool isFinalBlock = bytesRead == 0 || jsonStream.Position == jsonStream.Length;

                var reader = new Utf8JsonReader(buffer.AsSpan(0, bytesInBuffer), isFinalBlock, state);

                while (reader.Read()) {
                    if (reader.TokenType == JsonTokenType.PropertyName) {
                        if (matchIndex < path.Length && reader.ValueTextEquals(path[matchIndex])) {
                            matchIndex++;
                            currentPathDepth = reader.CurrentDepth;

                            if (matchIndex == path.Length) {
                                if (reader.Read()) {
                                    string? result = reader.TokenType switch {
                                        JsonTokenType.String => reader.GetString(),
                                        JsonTokenType.Number => reader.GetDouble().ToString(CultureInfo.InvariantCulture),
                                        JsonTokenType.True => "true",
                                        JsonTokenType.False => "false",
                                        JsonTokenType.Null => null,
                                        _ => reader.GetString()
                                    };
                                    return result;
                                }
                            } else {
                                reader.Read();
                                if (reader.TokenType == JsonTokenType.StartArray) {
                                    continue;
                                }
                            }
                        } else if (reader.CurrentDepth <= currentPathDepth && matchIndex > 0) {
                            matchIndex = 0;
                            currentPathDepth = 0;
                        }
                    }
                }

                state = reader.CurrentState;
                int bytesConsumed = (int)reader.BytesConsumed;

                buffer.AsSpan(bytesConsumed, bytesInBuffer - bytesConsumed).CopyTo(buffer);
                bytesInBuffer -= bytesConsumed;

                if (bytesInBuffer == buffer.Length) {
                    byte[] newBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                    buffer.AsSpan(0, bytesInBuffer).CopyTo(newBuffer);
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                    buffer = newBuffer;
                }

                if (isFinalBlock) break;
            }

            return null;
        } finally {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
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