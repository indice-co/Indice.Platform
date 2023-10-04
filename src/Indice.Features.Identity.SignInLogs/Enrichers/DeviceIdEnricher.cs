using Indice.Features.Identity.Core;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

/// <summary></summary>
public sealed class DeviceIdEnricher : ISignInLogEntryEnricher
{
    private readonly IDeviceIdResolver _mfaDeviceIdResolver;

    /// <summary></summary>
    /// <param name="mfaDeviceIdResolver"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public DeviceIdEnricher(IDeviceIdResolver mfaDeviceIdResolver) {
        _mfaDeviceIdResolver = mfaDeviceIdResolver ?? throw new ArgumentNullException(nameof(mfaDeviceIdResolver));
    }

    /// <inheritdoc />
    public int Order => 1;
    /// <inheritdoc />
    public SignInLogEnricherRunType RunType => SignInLogEnricherRunType.Synchronous;

    /// <inheritdoc />
    public async ValueTask EnrichAsync(SignInLogEntry logEntry) {
        var device = await _mfaDeviceIdResolver.Resolve();
        logEntry.DeviceId = device.Value;
        if (device.HasRegistrationId) {
            logEntry.ExtraData.UserDevice = new SignInLogEntryUserDevice {
                Id = device.RegistrationId.Value
            };
        }
    }
}
