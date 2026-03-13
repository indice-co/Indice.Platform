using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.ActivityLogs.Enrichers;

/// <summary>Enriches the activity log entry with the request id.</summary>
public sealed class RequestIdEnricher : IActivityLogEntryEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates a new instance of <see cref="RequestIdEnricher"/> class.</summary>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public RequestIdEnricher(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public int Order => 2;
    /// <inheritdoc />
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Synchronous;

    /// <inheritdoc />
    public ValueTask EnrichAsync(ActivityLogEntry logEntry) {
        logEntry.RequestId = _httpContextAccessor.HttpContext!.TraceIdentifier;
        return ValueTask.CompletedTask;
    }
}
