using Indice.AspNetCore.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Indice.AspNetCore.Features.MultiRateLimiter;

/// <summary>
/// Service that manages rate limiting policies and stores Rate Limiters in memory.
/// </summary>
public interface IMultiRateLimiterService
{
    /// <summary>
    /// Adds a rate limiting policy.
    /// </summary>
    /// <param name="policyName">The name of the policy.</param>
    /// <param name="configurePolicy">Function that configures the rate limit partition for the policy.</param>
    void AddPolicy(string policyName, Func<HttpContext, RateLimitPartition<string>> configurePolicy);

    /// <summary>
    /// Gets a rate limiting policy by name.
    /// </summary>
    /// <param name="policyName">The name of the policy.</param>
    /// <returns>The policy function if found, otherwise null.</returns>
    Func<HttpContext, RateLimitPartition<string>>? GetPolicy(string policyName);

    /// <summary>
    /// Gets all registered policy names.
    /// </summary>
    /// <returns>A read-only collection of policy names.</returns>
    IReadOnlyCollection<string> GetPolicyNames();

    /// <summary>
    /// Returns the Rate Limiter associated with a key. Creates it first if it doesn't exist.
    /// </summary>
    /// <param name="key">The identifier used to store the Rate Limiter.</param>
    /// <param name="factory">Function that creates the Rate Limiter.</param>
    public System.Threading.RateLimiting.RateLimiter GetOrCreateLimiter(string key, Func<System.Threading.RateLimiting.RateLimiter> factory);
}


/// <summary>
/// Service that manages rate limiting policies and stores Rate Limiters in memory.
/// </summary>
public class MultiRateLimiterService : IMultiRateLimiterService
{
    private static readonly ConcurrentDictionary<string, System.Threading.RateLimiting.RateLimiter> _limiters = new();
    private readonly ConcurrentDictionary<string, Func<HttpContext, RateLimitPartition<string>>> _policies = new();


    /// <summary>
    /// Helper extension to add policies to the service with a rule.
    /// </summary>
    public void AddPolicy(string policyName, RateLimiterEndpointRule rule, string userIdentifierClaimType = ClaimTypes.NameIdentifier)
    {
        AddPolicy(policyName, context =>
        {
            if (!rule.CanLimitHttpMethod(context.Request.Method))
            {
                return RateLimitPartition.GetNoLimiter("NoRateLimiting");
            }

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: RateLimiterExtensions.GetPartitionKey(context, userIdentifierClaimType, rule),
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

    /// <summary>
    /// Adds a rate limiting policy.
    /// </summary>
    /// <param name="policyName">The name of the policy.</param>
    /// <param name="configurePolicy">Function that configures the rate limit partition for the policy.</param>
    public void AddPolicy(string policyName, Func<HttpContext, RateLimitPartition<string>> configurePolicy)
    {
        _policies[policyName] = configurePolicy;
    }

    /// <summary>
    /// Gets a rate limiting policy by name.
    /// </summary>
    /// <param name="policyName">The name of the policy.</param>
    /// <returns>The policy function if found, otherwise null.</returns>
    public Func<HttpContext, RateLimitPartition<string>>? GetPolicy(string policyName)
    {
        return _policies.TryGetValue(policyName, out var policy) ? policy : null;
    }

    /// <summary>
    /// Gets all registered policy names.
    /// </summary>
    /// <returns>A read-only collection of policy names.</returns>
    public IReadOnlyCollection<string> GetPolicyNames()
    {
        return _policies.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Returns the Rate Limiter associated with a key. Creates it first if it doesn't exist.
    /// </summary>
    /// <param name="key">The identifier used to store the Rate Limiter.</param>
    /// <param name="factory">Function that creates the Rate Limiter.</param>
    public System.Threading.RateLimiting.RateLimiter GetOrCreateLimiter(string key, Func<System.Threading.RateLimiting.RateLimiter> factory)
    {
        return _limiters.GetOrAdd(key, _ => factory());
    }
}
