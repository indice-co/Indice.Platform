using System.Security.Claims;
using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;
using Indice.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting; // Added for IWebHostEnvironment
using Microsoft.Extensions.Configuration; // Added for IConfiguration
using Microsoft.Identity.Client;

namespace Indice.Features.ActivityLogs.Enrichers;

/// <summary>
/// Enriches activity log entries with request-specific information such as action name, request ID and IP address.
/// </summary>
public class RequestInfoEnricher : IActivityLogEntryEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _environment;

    /// <summary>Creates a new instance of <see cref="RequestInfoEnricher"/> class.</summary>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
    /// <param name="environment">Provides access to the hosting environment details.</param>
    /// <param name="configuration">Provides access to application configuration.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public RequestInfoEnricher(
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment environment,
        IConfiguration configuration) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <inheritdoc />
    public int Order => 4;

    /// <inheritdoc />
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Synchronous;

    /// <inheritdoc />
    public ValueTask EnrichAsync(ActivityLogEntry logEntry) {
        var context = _httpContextAccessor.HttpContext;
        logEntry.ApplicationName = _environment.ApplicationName;
        if (context is not null) {
            logEntry.ActionName = context.Request.RouteValues["action"]?.ToString() ?? context.GetEndpoint()?.DisplayName;
            logEntry.RequestId = context.TraceIdentifier;
            logEntry.IpAddress = context.Connection.RemoteIpAddress?.ToString();

            logEntry.HttpMethod = context.Request.Method;
            logEntry.RequestPath = context.Request.Path;
            logEntry.UserAgent = context.Request.Headers.UserAgent.ToString();
        }

        return ValueTask.CompletedTask;
    }
}