using System.Security.Claims;
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
        // If not authenticated, try to get email from request body
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
            if (httpContext.Request.HasFormContentType && httpContext.Request.Method == "POST") {
                if (httpContext.Request.Form.TryGetValue(partitionByProperty, out var emailValue)) {
                    partitionKey = emailValue.ToString();
                }
            }
            // Handle JSON content
            else if (httpContext.Request.ContentType?.Contains("application/json") == true) {
                using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
                var body = reader.ReadToEndAsync().GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(body)) {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(body);
                    if (jsonDoc.RootElement.TryGetProperty(partitionByProperty, out var emailElement)) {
                        partitionKey = emailElement.GetString();
                    }
                }
            }
        } catch {
            // fall back to IP or Host
        } finally {
            httpContext.Request.Body.Position = 0;
        }
        // Fallback to IP or Host
        partitionKey ??= httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString();
        return partitionKey;
    }
}