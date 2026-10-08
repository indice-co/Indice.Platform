using System.Security.Claims;
using System.Threading.RateLimiting;
using Indice.AspNetCore.Configuration;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
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
                        partitionKey: context.User.FindFirstValue(rateLimiterOptions.UserIdentifierClaimType) ?? context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
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
}