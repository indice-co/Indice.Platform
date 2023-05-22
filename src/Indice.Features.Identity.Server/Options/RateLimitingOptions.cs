using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Server.Options;

/// <summary>Rate limiting options for Identity Server API.</summary>
public class RateLimitingOptions
{
    /// <summary>Section name.</summary>
    public const string SectionName = "IdentityServer:RateLimiting";
    /// <summary>The default status code to set on the response when a request is rejected.</summary>
    public int? RejectionStatusCode { get; set; } = StatusCodes.Status429TooManyRequests;
    /// <summary>Rate limiting fixed window options for Identity Server API.</summary>
    public RateLimitingFixedWindowOptions FixedWindow { get; set; } = new RateLimitingFixedWindowOptions();
}

/// <summary>Rate limiting fixed window options for Identity Server API.</summary>
public class RateLimitingFixedWindowOptions
{
    /// <summary>Maximum number of permit counters that can be allowed in a window. Defaults to 4.</summary>
    public int? PermitLimit { get; set; } = 4;
    /// <summary>Maximum cumulative permit count of queued acquisition requests. Defaults to 0.</summary>
    public int? QueueLimit { get; set; } = 0;
    /// <summary>Determines the behavior of RateLimiter.AcquireAsync when not enough resources can be leased. Defaults to 'OldestFirst'.</summary>
    public QueueProcessingOrder? QueueProcessingOrder { get; set; } = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
    /// <summary>Specifies the time window that takes in the requests. Defaults to 1s.</summary>
    public TimeSpan? Window { get; set; } = TimeSpan.FromSeconds(1);
}
