using Indice.Features.ActivityLogs.Models;
using Indice.Security;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.ActivityLogs.Enrichers;

/// <summary>Enriches the activity log with user agent information.</summary>
public sealed class DeviceIdEnricher : IActivityLogEntryEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates a new instance of <see cref="DeviceEnricher"/> class.</summary>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DeviceIdEnricher(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public int Order => 4;
    /// <inheritdoc />
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Synchronous;

    /// <inheritdoc />
    public ValueTask EnrichAsync(ActivityLogEntry logEntry) {
        logEntry.DeviceId ??= _httpContextAccessor.HttpContext?.User?.FindFirst(BasicClaimTypes.DeviceId)?.Value;
        return ValueTask.CompletedTask;
    }
}