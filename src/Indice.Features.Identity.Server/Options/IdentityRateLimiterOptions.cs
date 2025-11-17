using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Polly;

namespace Indice.Features.Identity.Server.Options;

/// <summary>Rate limiter options for Identity Server API.</summary>
public class IdentityRateLimiterOptions
{
    /// <summary>Section name.</summary>
    public const string SectionName = "IdentityServer:RateLimiter";
    /// <summary>The default status code to set on the response when a request is rejected.</summary>
    public int? RejectionStatusCode { get; set; } = StatusCodes.Status429TooManyRequests;
    /// <summary>Rate limiter fixed window options for Identity Server API.</summary>
    public RateLimiterEndpointRule[] Rules { get; set; } = Array.Empty<RateLimiterEndpointRule>();
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

    /// <summary>Default configuration for <see cref="RateLimiterEndpointRule"/>.</summary>
    public static RateLimiterEndpointRule Default(string? policyName = null) => policyName switch {
        "secure-page" => new() { PermitLimit = 5, Window = TimeSpan.FromSeconds(1), HttpMethod = "POST" },
        "forgot-password" => new() { PermitLimit = 5, Window = TimeSpan.FromSeconds(1), HttpMethod = "POST" },
        "login" => new() { PermitLimit = 5, Window = TimeSpan.FromSeconds(1), HttpMethod = "POST" },
        "register" => new() { PermitLimit = 5, Window = TimeSpan.FromSeconds(1), HttpMethod = "POST" },
        _ => new()
    };

    /// <summary>The Http method of the endpoint to apply the rate limiter. Optional.</summary>
    public string? HttpMethod { get; set; }

    /// <summary>Determines whether <see cref="HttpMethod"/> has a value.</summary>
    public bool HasHttpMehtod => !string.IsNullOrWhiteSpace(HttpMethod);

    /// <summary>Determines whether the rate limiter can be applied based on the http method.</summary>
    public bool CanLimitHttpMethod(string? httpMethod) =>
        !HasHttpMehtod || string.Equals(HttpMethod, httpMethod, StringComparison.OrdinalIgnoreCase);
}
