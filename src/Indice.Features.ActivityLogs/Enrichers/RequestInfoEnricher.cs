using Indice.Features.ActivityLogs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting; // Added for IWebHostEnvironment
using Microsoft.Extensions.Configuration; // Added for IConfiguration

namespace Indice.Features.ActivityLogs.Enrichers;

/// <summary>
/// Enriches activity log entries with request-specific information such as action name, request ID and IP address.
/// </summary>
public class RequestInfoEnricher : IActivityLogEntryEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates a new instance of <see cref="RequestInfoEnricher"/> class.</summary>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public RequestInfoEnricher(
        IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public int Order => 4;

    /// <inheritdoc />
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Synchronous;

    /// <inheritdoc />
    public ValueTask EnrichAsync(ActivityLogEntry logEntry) {
        var context = _httpContextAccessor.HttpContext;
        if (context is not null) {
            logEntry.ActionName ??= context.GetEndpoint()?.DisplayName ?? context.Request.Path;
            logEntry.RequestId ??= context.TraceIdentifier;
            logEntry.IpAddress ??= context.Connection.RemoteIpAddress?.ToString();
        }
        return ValueTask.CompletedTask;
    }
}