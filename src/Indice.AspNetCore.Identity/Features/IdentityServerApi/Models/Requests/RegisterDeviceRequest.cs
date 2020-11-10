using System;
using System.ComponentModel.DataAnnotations;
using Indice.Types;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Register a device for push notifications
    /// </summary>
    public class RegisterDeviceRequest
    {
        /// <summary>
        /// The deviceId to register for push notifications
        /// </summary>
        [Required]
        public Guid DeviceId { get; set; }
        /// <summary>
        /// Platform Notification Service(pns) obtained from client platform
        /// </summary>
        [Required]
        public string PnsHandle { get; set; }
        /// <summary>
        /// Device name
        /// </summary>
        [Required]
        public string DeviceName { get; set; }
        /// <summary>
        /// Client device platform
        /// </summary>
        [Required]
        public DevicePlatform DevicePlatform { get; set; }
    }
}
