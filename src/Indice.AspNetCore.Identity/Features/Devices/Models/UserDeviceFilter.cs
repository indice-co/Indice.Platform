namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>
    /// Contains filter when querying for user device list.
    /// </summary>
    public class UserDeviceFilter
    {
        /// <summary>
        /// Returns all the devices (value = null), the enabled devices (value = true) or the disabled devices (value = false).
        /// </summary>
        public bool? IsPushNotificationEnabled { get; set; }
    }
}