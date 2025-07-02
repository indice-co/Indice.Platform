using Indice.Types;
using Indice.Features.Identity.Core.Types;
using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.Core.Data.Models;

/// <summary>User devices representation.</summary>
public class UserDevice
{
    /// <summary>Constructs a new instance of <see cref="UserDevice"/> with a new <see cref="Guid"/> as Id.</summary>
    public UserDevice() : this(Guid.NewGuid()) { }

    /// <summary>Constructs a new instance of <see cref="UserDevice"/> using the given <see cref="Guid"/> as Id.</summary>
    /// <param name="id">The primary key.</param>
    public UserDevice(Guid id) => Id = id;

    /// <summary>The primary key.</summary>
    public Guid Id { get; }
    /// <summary>Device id.</summary>
    public string DeviceId { get; set; } = null!;
    /// <summary>The user id related to this device.</summary>
    public string UserId { get; set; } = null!;
    /// <summary>Device operating system.</summary>
    public DevicePlatform Platform { get; set; }
    /// <summary>Device name.</summary>
    public string? Name { get; set; }
    /// <summary>Device model.</summary>
    public string? Model { get; set; }
    /// <summary>Device OS version.</summary>
    public string? OsVersion { get; set; }
    /// <summary>The date this device was created.</summary>
    public DateTimeOffset DateCreated { get; set; }
    /// <summary>Gets or sets the date and time, in UTC, when the device last signed in.</summary>
    public DateTimeOffset? LastSignInDate { get; set; }
    /// <summary>Flag that determines if push notifications are enabled for this device.</summary>
    public bool IsPushNotificationsEnabled { get; set; }
    /// <summary>Associated password for device (when <see cref="InteractionMode"/> is equal to <see cref="InteractionMode.Pin"/>).</summary>
    public string? Password { get; set; }
    /// <summary>Flag for pin support.</summary>
    public bool SupportsPinLogin => !string.IsNullOrWhiteSpace(Password);
    /// <summary>Device public key (when <see cref="InteractionMode"/> is equal to <see cref="InteractionMode.Fingerprint"/>).</summary>
    public string? PublicKey { get; set; }
    /// <summary>Flag for fingerprint support.</summary>
    public bool SupportsFingerprintLogin => !string.IsNullOrWhiteSpace(PublicKey);
    /// <summary>Extra metadata for the device.</summary>
    public dynamic? Data { get; set; }
    /// <summary>Platform notification service handle.</summary>
    public string? PnsHandle { get; set; }
    /// <summary>Device tags.</summary>
    public string[]? Tags { get; set; }
    /// <summary>Indicates whether the device is blocked.</summary>
    public bool RequiresPassword { get; set; }
    /// <summary>Indicates whether the device is a trusted device (i.e. capable of strong customer authentication scenarios).</summary>
    public bool IsTrusted { get; set; }
    /// <summary>The date that the device can be activated for trust.</summary>
    public DateTimeOffset? TrustActivationDate { get; set; }
    /// <summary>Determines whether the device is pending trust activation.</summary>
    public bool IsPendingTrustActivation => TrustActivationDate.HasValue && TrustActivationDate.Value > DateTimeOffset.UtcNow;
    /// <summary>Indicates whether device is blocked for any action.</summary>
    public bool Blocked { get; set; }
    /// <summary>Describes the type of a user device.</summary>
    public DeviceClientType? ClientType { get; set; }
    /// <summary>The date until the client is remembered by the system and MFA is not asked.</summary>
    public DateTimeOffset? MfaSessionExpirationDate { get; set; }
    /// <summary>Determines whether the device has an active MFA session. This is the period of time that the device is remembered by the system and MFA is not asked.</summary>
    public bool MfaSessionActive() => IsTrusted && (!MfaSessionExpirationDate.HasValue || MfaSessionExpirationDate >= DateTimeOffset.UtcNow);

    /// <summary>
    /// Renews the trust relationship by updating trust-related properties and extending the multi-factor authentication
    /// (MFA) session expiration.
    /// </summary>
    /// <remarks>This method ensures that the trust relationship is activated if it is not already active. It
    /// updates the trust activation date, the last sign-in date, and the MFA session expiration date based on the
    /// provided or default reference time.</remarks>
    /// <param name="mfaRememberDurationInDays">The number of days to extend the MFA session expiration from the current time.</param>
    /// <param name="asOfDate">An optional date and time to use as the reference point for the operation. If not provided, the current UTC time
    /// is used.</param>
    public void RenewTrust(int mfaRememberDurationInDays, DateTimeOffset? asOfDate = null) {
        asOfDate ??= DateTimeOffset.UtcNow;
        TrustActivationDate ??= asOfDate;
        if (!IsTrusted) {
            IsTrusted = true;
            TrustActivationDate = asOfDate;
        }
        MfaSessionExpirationDate = asOfDate.Value.AddDays(mfaRememberDurationInDays);
        LastSignInDate = asOfDate;
    }

    /// <summary>The user associated with this device.</summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userAgentHeader"></param>
    /// <param name="deviceId"></param>
    /// <param name="userId"></param>
    /// <param name="mfaRememberDurationInDays"></param>
    /// <param name="asOfDate"></param>
    /// <param name="deviceClientType"></param>
    /// <returns></returns>
    public static UserDevice FromUserAgent(string userAgentHeader, MfaDeviceIdentifier deviceId, string userId, int mfaRememberDurationInDays, DateTimeOffset? asOfDate = null, DeviceClientType? deviceClientType = DeviceClientType.Browser) {
        ArgumentNullException.ThrowIfNull(userAgentHeader);
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId.Value);
        var userAgent = new UserAgent(userAgentHeader);
        asOfDate ??= DateTimeOffset.UtcNow;
        return new UserDevice {
            ClientType = deviceClientType,
            DateCreated = asOfDate.Value,
            DeviceId = deviceId.Value!,
            IsTrusted = true,
            LastSignInDate = asOfDate,
            MfaSessionExpirationDate = asOfDate.Value.AddDays(mfaRememberDurationInDays),
            Model = userAgent.DeviceModel,
            Name = userAgent.DisplayName,
            OsVersion = userAgent.Os,
            Platform = userAgent.DevicePlatform,
            TrustActivationDate = asOfDate,
            UserId = userId
        };
    }   
}

/// <summary>Models the way a device interacts with the identity system for trusted authorization.</summary>
public enum InteractionMode
{
    /// <summary>Fingerprint</summary>
    Fingerprint,
    /// <summary>4-pin</summary>
    Pin
}
