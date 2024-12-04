﻿using Indice.Features.Identity.Core.Data.Models;
using Indice.Types;

namespace Indice.Features.Identity.Core.Events.Models;

/// <summary>Device information about an event that occurred.</summary>
public class UserDeviceEventContext
{
    /// <summary>The primary key.</summary>
    public Guid Id { get; private set; }
    /// <summary>Device id.</summary>
    public string DeviceId { get; private set; } = null!;
    /// <summary>Device operating system.</summary>
    public DevicePlatform Platform { get; private set; }
    /// <summary>Device name.</summary>
    public string? Name { get; private set; }
    /// <summary>Device model.</summary>
    public string? Model { get; private set; }
    /// <summary>Device OS version.</summary>
    public string? OsVersion { get; private set; }
    /// <summary>The date this device was created.</summary>
    public DateTimeOffset DateCreated { get; private set; }
    /// <summary>Gets or sets the date and time, in UTC, when the device last signed in.</summary>
    public DateTimeOffset? LastSignInDate { get; private set; }
    /// <summary>Flag that determines if push notifications are enabled for this device.</summary>
    public bool IsPushNotificationsEnabled { get; private set; }
    /// <summary>Extra metadata for the device.</summary>
    public dynamic? Data { get; private set; }
    /// <summary>Device tags.</summary>
    public string[] Tags { get; private set; } = [];
    /// <summary>Indicates whether the device is blocked.</summary>
    public bool RequiresPassword { get; private set; }
    /// <summary>Indicates whether the device is a trusted device (i.e. capable of strong customer authentication scenarios).</summary>
    public bool IsTrusted { get; private set; }
    /// <summary>The date that the device can be activated for trust.</summary>
    public DateTimeOffset? TrustActivationDate { get; private set; }
    /// <summary>Indicates whether device is blocked for any action.</summary>
    public bool Blocked { get; private set; }
    /// <summary>Describes the type of a user device.</summary>
    public DeviceClientType? ClientType { get; private set; }
    /// <summary>The date until the client is remembered by the system and MFA is not asked.</summary>
    public DateTimeOffset? MfaSessionExpirationDate { get; private set; }

    /// <summary>Creates a new <see cref="UserEventContext"/> instance given a <see cref="User"/> entity.</summary>
    /// <param name="device">The device entity.</param>
    public static UserDeviceEventContext InitializeFromUserDevice(UserDevice device) => new() {
        Blocked = device.Blocked,
        ClientType = device.ClientType,
        Tags = device.Tags ?? Array.Empty<string>(),
        Data = device.Data,
        DateCreated = device.DateCreated,
        DeviceId = device.DeviceId,
        Id = device.Id,
        IsPushNotificationsEnabled = device.IsPushNotificationsEnabled,
        IsTrusted = device.IsTrusted,
        LastSignInDate = device.LastSignInDate,
        MfaSessionExpirationDate = device.MfaSessionExpirationDate,
        Model = device.Model,
        Name = device.Name,
        OsVersion = device.OsVersion,
        Platform = device.Platform,
        RequiresPassword = device.RequiresPassword,
        TrustActivationDate = device.TrustActivationDate
    };
}
