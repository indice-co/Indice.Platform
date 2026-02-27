using System.Threading.RateLimiting;
using Indice.Security;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Configuration;

/// <summary>Rate limiter options for Identity Server API.</summary>
public class RateLimiterOptions
{
    /// <summary>Section name.</summary>
    public string SectionName = "RateLimiter";
    /// <summary>User identifier claim type.</summary>
    public string UserIdentifierClaimType = BasicClaimTypes.Subject;
    /// <summary>The default status code to set on the response when a request is rejected.</summary>
    public int? RejectionStatusCode { get; set; } = StatusCodes.Status429TooManyRequests;
    /// <summary>Rate limiter fixed window options for Identity Server API.</summary>
    public RateLimiterEndpointRule[] Rules { get; set; } = Array.Empty<RateLimiterEndpointRule>();
    /// <summary>List of all rate limiter policies. This is used to ensure that all policies are registered in the rate limiter middleware.</summary>
    public IReadOnlyList<string> AllRateLimiterPolicies { get; set; } = Array.Empty<string>();

    /// <summary>Custom factory function for creating policy-specific rate limiter rules. Set this to provide custom configurations based on policy names.</summary>
    public Func<string, RateLimiterEndpointRule>? CustomPolicyFactory { get; set; }

    /// <summary>Default configuration for <see cref="RateLimiterEndpointRule"/>. Returns custom rule if <see cref="CustomPolicyFactory"/> is set, otherwise returns a default rule.</summary>
    /// <param name="policyName">The policy name to get the configuration for.</param>
    public RateLimiterEndpointRule GetPolicySettings(string policyName) =>
        CustomPolicyFactory?.Invoke(policyName) ?? new();
}

/// <summary>Rate limiter fixed window options for Identity Server API.</summary>
public class RateLimiterEndpointRule
{
    /// <summary>The endpoint name.</summary>
    public string? Endpoint { get; set; }
    /// <summary>Maximum number of permit counters that can be allowed in a window. Defaults to 4.</summary>
    public int? PermitLimit { get; set; } = 4;
    /// <summary>Maximum cumulative permit count of queued acquisition requests. Defaults to 0.</summary>
    public int? QueueLimit { get; } = 0;
    /// <summary>Determines the behavior of RateLimiter.AcquireAsync when not enough resources can be leased. Defaults to 'OldestFirst'.</summary>
    public QueueProcessingOrder? QueueProcessingOrder { get; } = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
    /// <summary>Specifies the time window that takes in the requests. Defaults to 1s.</summary>
    public TimeSpan? Window { get; set; } = TimeSpan.FromSeconds(1);


    /// <summary>The Http method of the endpoint to apply the rate limiter. Optional.</summary>
    public string? HttpMethod { get; set; }

    /// <summary>Determines whether <see cref="HttpMethod"/> has a value.</summary>
    public bool HasHttpMehtod => !string.IsNullOrWhiteSpace(HttpMethod);

    /// <summary>Determines whether the rate limiter can be applied based on the http method.</summary>
    public bool CanLimitHttpMethod(string? httpMethod) =>
        !HasHttpMehtod || string.Equals(HttpMethod, httpMethod, StringComparison.OrdinalIgnoreCase);
}
