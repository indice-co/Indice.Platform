using Indice.AspNetCore.Identity.Data.Models;
using Indice.Types;

namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>
    /// Models a user device.
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// Device id.
        /// </summary>
        public string DeviceId { get; set; }
        /// <summary>
        /// Device name.
        /// </summary>
        public string DeviceName { get; set; }
        /// <summary>
        /// Device operating system.
        /// </summary>
        public DevicePlatform DevicePlatform { get; set; }
        /// <summary>
        /// Flag that determines if push notifications are enabled for this device.
        /// </summary>
        public bool IsPushNotificationsEnabled { get; set; }

        /// <summary>
        /// Creates a new instace of <see cref="DeviceInfo"/> from a <see cref="UserDevice"/> object.
        /// </summary>
        /// <param name="device">The device instance.</param>
        public static DeviceInfo FromUserDevice(UserDevice device) => new() {
            DeviceId = device.DeviceId,
            DeviceName = device.DeviceName,
            DevicePlatform = device.DevicePlatform,
            IsPushNotificationsEnabled = device.IsPushNotificationsEnabled
        };
    }
}
