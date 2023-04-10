using System.Linq.Expressions;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Types;

namespace Indice.Features.Identity.Server.Devices.Models;
/// <summary>Models a user device.</summary>
public class DeviceInfo
{
    /// <summary>Device id.</summary>
    public string? DeviceId { get; set; }
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
    /// <summary>Flag for pin support.</summary>
    public bool SupportsPinLogin { get; set; }
    /// <summary>Flag for fingerprint support.</summary>
    public bool SupportsFingerprintLogin { get; set; }
    /// <summary>Indicates whether the device is blocked.</summary>
    public bool RequiresPassword { get; set; }
    /// <summary>The date that the device can be activated for trust.</summary>
    public DateTimeOffset? TrustActivationDate { get; set; }
    /// <summary>Indicates whether the device is a trusted device (i.e. capable of strong customer authentication scenarios).</summary>
    public bool IsTrusted { get; set; }
    /// <summary>Indicates whether the user can activate device trust after waiting for the specified delay.</summary>
    public bool CanActivateDeviceTrust => TrustActivationDate.HasValue && TrustActivationDate.Value <= DateTimeOffset.UtcNow && !IsTrusted;
    /// <summary>Extra metadata for the device.</summary>
    public dynamic? Data { get; set; }
    /// <summary>Describes the type of a user device.</summary>
    public DeviceClientType? ClientType { get; set; }
}

internal static class DeviceInfoExtensions
{
    public static Expression<Func<UserDevice, DeviceInfo>> ToDeviceInfo = (UserDevice device) => new DeviceInfo {
        ClientType = device.ClientType,
        Data = device.Data,
        DateCreated = device.DateCreated,
        DeviceId = device.DeviceId,
        IsPushNotificationsEnabled = device.IsPushNotificationsEnabled,
        IsTrusted = device.IsTrusted,
        LastSignInDate = device.LastSignInDate,
        Model = device.Model,
        Name = device.Name,
        OsVersion = device.OsVersion,
        Platform = device.Platform,
        RequiresPassword = device.RequiresPassword,
        SupportsFingerprintLogin = device.SupportsFingerprintLogin,
        SupportsPinLogin = device.SupportsPinLogin,
        TrustActivationDate = device.TrustActivationDate
    };
}