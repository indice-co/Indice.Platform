namespace Indice.AspNetCore.Identity.Data.Models
{
    /// <summary>Contains filter when querying for user device list.</summary>
    public class UserDeviceListFilter
    {
        /// <summary>Returns all the devices (value = null), the push notification enabled devices (value = true) or the push notification disabled devices (value = false).</summary>
        public bool? IsPushNotificationEnabled { get; set; }
        /// <summary>Returns all the devices (value = null), the trusted devices (value = true) or the untrusted devices (value = false).</summary>
        public bool? IsTrusted { get; set; }
        /// <summary>Returns all the devices (value = null), the blocked devices (value = true) or the non blocked devices (value = false).</summary>
        public bool? Blocked { get; set; }
        /// <summary>Describes the possible types of a user device.</summary>
        public UserDeviceType? UserDeviceType { get; set; }
        /// <summary>Determines whether the device is pending trust activation.</summary>
        public bool? IsPendingTrustActivation { get; set; }

        /// <summary>Filter trusted devices.</summary>
        public static UserDeviceListFilter TrustedNativeDevices() => new() {
            Blocked = false,
            IsPendingTrustActivation = false,
            IsTrusted = true,
            UserDeviceType = Models.UserDeviceType.Native
        };

        /// <summary>Filter trusted or pending trust activation native devices.</summary>
        public static UserDeviceListFilter TrustedOrPendingNativeDevices() => new() {
            Blocked = false,
            IsPendingTrustActivation = true,
            IsTrusted = true,
            UserDeviceType = Models.UserDeviceType.Native
        };

        /// <summary>Filter native devices.</summary>
        public static UserDeviceListFilter NativeDevices() => new() {
            UserDeviceType = Models.UserDeviceType.Native
        };

        /// <summary>Filter native devices.</summary>
        public static UserDeviceListFilter TrustedBrowsers() => new() {
            IsTrusted = true,
            UserDeviceType = Models.UserDeviceType.Browser
        };
    }
}
