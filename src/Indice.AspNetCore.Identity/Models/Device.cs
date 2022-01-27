using System;
using System.Collections.Generic;
using Indice.Types;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Models a user device.
    /// </summary>
    public class Device
    {
        /// <summary>
        /// The device id to register for push notifications.
        /// </summary>
        public string DeviceId { get; set; }
        /// <summary>
        /// Platform Notification Service (PNS) obtained from client platform.
        /// </summary>
        public string PnsHandle { get; set; }
        /// <summary>
        /// Device name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The date this device was created.
        /// </summary>
        public DateTimeOffset? DateCreated { get; set; }
        /// <summary>
        /// Client device platform.
        /// </summary>
        public DevicePlatform Platform { get; set; }
        /// <summary>
        /// Tags are used to route notifications to the correct set of device handles.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        /// <summary>
        /// Device model.
        /// </summary>
        public string Model { get; set; }
        /// <summary>
        /// Device OS version.
        /// </summary>
        public string OsVersion { get; set; }
        /// <summary>
        /// Extra metadata for the device.
        /// </summary>
        public dynamic Data { get; set; }
    }
}
