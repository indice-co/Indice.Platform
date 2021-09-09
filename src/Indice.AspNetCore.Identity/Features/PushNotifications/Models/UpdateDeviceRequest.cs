using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>
    /// Register a device for push notifications.
    /// </summary>
    public class UpdateDeviceRequest
    {
        /// <summary>
        /// Device name.
        /// </summary>
        [Required(AllowEmptyStrings = false), MaxLength(250)]
        public string DeviceName { get; set; }
        /// <summary>
        /// Indicates whether push notifications are enabled for the device.
        /// </summary>
        public bool IsPushNotificationsEnabled { get; set; }
        /// <summary>
        /// Tags are used to route notifications to the correct set of device handles.
        /// </summary>
        public List<string> Tags { get; set; }
        /// <summary>
        /// Platform Notification Service (PNS) obtained from client platform.
        /// </summary>
        public string PnsHandle { get; set; }
    }
}
