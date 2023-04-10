using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

internal class DeviceEnricher : ISignInLogEntryEnricher
{
    private readonly ExtendedUserManager<User> _userManager;

    public DeviceEnricher(ExtendedUserManager<User> userManager) {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    public int Priority => 5;

    public EnricherDependencyType DependencyType => EnricherDependencyType.OnDataStore;

    public async Task EnrichAsync(SignInLogEntry logEntry) {
        if (string.IsNullOrWhiteSpace(logEntry.DeviceId)) {
            return;
        }
        logEntry.User ??= (!string.IsNullOrWhiteSpace(logEntry.SubjectId) ? await _userManager.FindByIdAsync(logEntry.SubjectId) : default);
        if (logEntry.User is null) {
            return;
        }
        var device = await _userManager.GetDeviceByIdAsync(logEntry.User, logEntry.DeviceId);
        if (device is null) {
            return;
        }
        logEntry.ExtraData.Device = new {
            device.Id,
            device.Blocked,
            device.ClientType,
            device.DateCreated,
            device.IsPendingTrustActivation,
            device.IsPushNotificationsEnabled,
            device.IsTrusted,
            device.LastSignInDate,
            device.MfaSessionExpirationDate,
            device.Model,
            device.Name,
            device.OsVersion,
            device.Platform,
            device.RequiresPassword,
            device.SupportsFingerprintLogin,
            device.SupportsPinLogin,
            device.Tags,
            device.TrustActivationDate,
            device.Data
        };
    }
}
