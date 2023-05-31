using Indice.Features.Identity.Core;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

internal class DeviceIdEnricher : ISignInLogEntryEnricher
{
    private readonly IMfaDeviceIdResolver _mfaDeviceIdResolver;

    public DeviceIdEnricher(IMfaDeviceIdResolver mfaDeviceIdResolver) {
        _mfaDeviceIdResolver = mfaDeviceIdResolver ?? throw new ArgumentNullException(nameof(mfaDeviceIdResolver));
    }

    public int Order => 1;

    public SignInLogEnricherRunType RunType => SignInLogEnricherRunType.Synchronous;

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
