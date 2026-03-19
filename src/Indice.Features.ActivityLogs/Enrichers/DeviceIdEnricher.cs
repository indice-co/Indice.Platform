using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;
using Indice.Features.Identity.Core.Extensions;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.ActivityLogs.Enrichers;

/// <summary>Enriches the activity log entry with the device id that performs the activity operation.</summary>
public sealed class DeviceIdEnricher : IActivityLogEntryEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates a new instance of <see cref="DeviceIdEnricher"/> class.</summary>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DeviceIdEnricher(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public int Order => 1;
    /// <inheritdoc />
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Synchronous;

    /// <inheritdoc />
    public ValueTask EnrichAsync(ActivityLogEntry logEntry) {
        var device = _httpContextAccessor.HttpContext.ResolveDeviceId();
        logEntry.DeviceId = device.Value;
        if (device.HasRegistrationId) {
            logEntry.ExtraData ??= new();
            logEntry.ExtraData.UserDevice = new ActivityLogEntryUserDevice {
                Id = device.RegistrationId!.Value
            };
        }
        return ValueTask.CompletedTask;
    }
}
