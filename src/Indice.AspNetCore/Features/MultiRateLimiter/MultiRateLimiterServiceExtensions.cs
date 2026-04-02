using System.Threading.RateLimiting;
using Indice.AspNetCore.Configuration;
using Indice.AspNetCore.Features.MultiRateLimiter;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the MultiRateLimiter service.
/// </summary>
public static class MultiRateLimiterServiceExtensions
{
    /// <summary>
    /// Registers the multi-rate limiter services with configuration binding.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configureOptions">An action to configure the base rate limiter options.</param>
    /// <param name="configurePolicies">An action to configure policies via the service.</param>
    /// <param name="configurationSectionName">The configuration section name. Optional.</param>
    public static IServiceCollection AddMultipleRateLimiter(this IServiceCollection services, IConfiguration configuration, Action<RateLimiterOptions>? configureOptions = null, Action<IMultiRateLimiterService>? configurePolicies = null, string? configurationSectionName = null)
    {
        var sectionName = configurationSectionName ?? "MultiRateLimiter";

        // Configure base RateLimiterOptions
        services.Configure<RateLimiterOptions>(configuration.GetSection(sectionName));
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register the service
        services.AddSingleton<IMultiRateLimiterService>(provider =>
        {
            var service = new MultiRateLimiterService();

            // Load configuration and auto-register policies from config
            var rateLimiterOptions = new RateLimiterOptions();
            configuration.GetSection(sectionName).Bind(rateLimiterOptions);
            configureOptions?.Invoke(rateLimiterOptions);

            // Register policies from configuration Rules
            foreach (var rule in rateLimiterOptions.Rules)
            {
                if (!string.IsNullOrEmpty(rule.Endpoint))
                {
                    service.AddPolicy(rule.Endpoint, context =>
                    {
                        if (!rule.CanLimitHttpMethod(context.Request.Method))
                        {
                            return RateLimitPartition.GetNoLimiter("NoRateLimiting");
                        }

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: RateLimiterExtensions.GetPartitionKey(context, rateLimiterOptions.UserIdentifierClaimType, rule),
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = rule.PermitLimit ?? 10,
                                QueueLimit = rule.QueueLimit ?? 0,
                                QueueProcessingOrder = rule.QueueProcessingOrder ?? QueueProcessingOrder.OldestFirst,
                                Window = rule.Window ?? TimeSpan.FromMinutes(1),
                                AutoReplenishment = true
                            });
                    });
                }
            }

            // Allow additional configuration via callback
            configurePolicies?.Invoke(service);
            return service;
        });

        return services;
    }

    /// <summary>
    /// Adds the MultiRateLimitingMiddleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseMultipleRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MultiRateLimitingMiddleware>();
    }

    /// <summary>
    /// Applies multiple rate limiting policies to a minimal API endpoint.
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="policyNames">The names of the rate limiting policies to apply.</param>
    /// <returns>The endpoint convention builder for chaining.</returns>
    public static TBuilder RequireMultiRateLimiting<TBuilder>(this TBuilder builder, params string[] policyNames)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.WithMetadata(new MultiRateLimitingMetadata(policyNames));
    }

}