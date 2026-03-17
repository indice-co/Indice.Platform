
using Indice.AspNetCore;
using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Indice.Features.ActivityLogs.Enrichers;

/// <summary>Enriches the activity log with user agent information.</summary>
public sealed class DeviceEnricher : IActivityLogEntryEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates a new instance of <see cref="DeviceEnricher"/> class.</summary>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DeviceEnricher(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public int Order => 4;
    /// <inheritdoc />
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Synchronous;

    /// <inheritdoc />
    public ValueTask EnrichAsync(ActivityLogEntry logEntry) {
        var userAgentHeader = _httpContextAccessor.HttpContext!.Request.Headers[HeaderNames.UserAgent];
        if (string.IsNullOrWhiteSpace(userAgentHeader)) {
            return ValueTask.CompletedTask;
        }
        var userAgent = new UserAgent(userAgentHeader!);
        logEntry.ExtraData ??= new();
        logEntry.ExtraData.Device = new ActivityLogEntryDevice {
            Model = userAgent.DeviceModel,
            Platform = userAgent.DevicePlatform,
            UserAgent = userAgent.HeaderValue,
            DisplayName = userAgent.DisplayName,
            Os = userAgent.Os,
            UserAgentFamily = userAgent.UserAgentFamily
        };
        return ValueTask.CompletedTask;
    }
}