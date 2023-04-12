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

    public int Priority => 1;

    public EnricherDependencyType DependencyType => EnricherDependencyType.OnRequest;

    public async ValueTask EnrichAsync(SignInLogEntry logEntry) {
        var deviceId = await _mfaDeviceIdResolver.Resolve();
        logEntry.DeviceId = deviceId;
    }
}
