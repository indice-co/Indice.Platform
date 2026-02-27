using System.Security.Claims;
using System.Threading.RateLimiting;
using Indice.AspNetCore.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>
/// 
/// </summary>
public static class RateLimiterExtensions
{
    /// <summary>Registers the rate limiter services.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configureOptions">An action to configure the rate limiter options.</param>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, 
        IConfiguration configuration, Action<RateLimiterOptions> configureOptions) {
        var myRateLimiterOptions = new RateLimiterOptions();
        configureOptions?.Invoke(myRateLimiterOptions);
        configuration.GetSection(myRateLimiterOptions.SectionName).Bind(myRateLimiterOptions);

        services.AddRateLimiter(rateLimiterOptions => {
            foreach (var endpoint in myRateLimiterOptions.AllRateLimiterPolicies) {
                var endpointOptions = myRateLimiterOptions.Rules.FirstOrDefault(rule => rule.Endpoint == endpoint) ?? myRateLimiterOptions.GetPolicySettings(endpoint);
                rateLimiterOptions.AddPolicy(endpoint, context => {
                    if (!endpointOptions.CanLimitHttpMethod(context.Request.Method)) {
                        return RateLimitPartition.GetNoLimiter("NoRateLimiting");
                    }
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.User.FindClaimValue(myRateLimiterOptions.UserIdentifierClaimType) ?? context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                        factory: _ => new FixedWindowRateLimiterOptions {
                            PermitLimit = endpointOptions.PermitLimit.GetValueOrDefault(),
                            QueueLimit = endpointOptions.QueueLimit.GetValueOrDefault(),
                            QueueProcessingOrder = endpointOptions.QueueProcessingOrder.GetValueOrDefault(),
                            Window = endpointOptions.Window.GetValueOrDefault()
                        });
                });
            }
            rateLimiterOptions.RejectionStatusCode = myRateLimiterOptions.RejectionStatusCode.GetValueOrDefault();
            rateLimiterOptions.OnRejected = (context, cancellationToken) => {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
                    context.HttpContext.Items.Add("retry-after", retryAfter.TotalSeconds);
                }
                return ValueTask.CompletedTask;
            };
        });
        return services;
    }

    /// <summary>Gets the user's unique id.</summary>
    /// <param name="principal">The current principal.</param>
    /// <param name="claimType"></param>
    public static string? FindClaimValue(this ClaimsPrincipal principal, string claimType) => principal.FindFirst(claimType)?.Value;
}

