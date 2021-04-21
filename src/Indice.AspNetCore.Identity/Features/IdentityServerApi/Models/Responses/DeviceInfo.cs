using Indice.Types;

namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>
    /// Models a user device
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// Device id
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Device name
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// Device operating system
        /// </summary>
        public DevicePlatform DevicePlatform { get; set; }

        /// <summary>
        /// Flag that determines if push notifications are enabled for this device
        /// </summary>
        public bool IsPushNotificationsEnabled { get; set; }
    }
}
