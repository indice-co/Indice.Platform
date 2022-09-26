using System;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Types;

namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>Models a user device.</summary>
    public class DeviceInfo
    {
        /// <summary>Device id.</summary>
        public string DeviceId { get; set; }
        /// <summary>Device name.</summary>
        public string Name { get; set; }
        /// <summary>Device operating system.</summary>
        public DevicePlatform Platform { get; set; }
        /// <summary>Flag that determines if push notifications are enabled for this device.</summary>
        public bool IsPushNotificationsEnabled { get; set; }
        /// <summary>The date this device was created.</summary>
        public DateTimeOffset DateCreated { get; set; }
        /// <summary>Device model.</summary>
        public string Model { get; set; }
        /// <summary>Device OS version.</summary>
        public string OsVersion { get; set; }
        /// <summary>Gets or sets the date and time, in UTC, when the device last signed in.</summary>
        public DateTimeOffset? LastSignInDate { get; set; }
        /// <summary>Extra metadata for the device.</summary>
        public dynamic Data { get; set; }

        /// <summary>Creates a new instance of <see cref="DeviceInfo"/> from a <see cref="UserDevice"/> object.</summary>
        /// <param name="device">The device instance.</param>
        public static DeviceInfo FromUserDevice(UserDevice device) => new() {
            Data = device.Data,
            DateCreated = device.DateCreated,
            DeviceId = device.DeviceId,
            IsPushNotificationsEnabled = device.IsPushNotificationsEnabled,
            LastSignInDate = device.LastSignInDate,
            Model = device.Model,
            Name = device.Name,
            OsVersion = device.OsVersion,
            Platform = device.Platform
        };
    }
}
