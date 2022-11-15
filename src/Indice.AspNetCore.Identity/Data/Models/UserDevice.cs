using System;
using Indice.Types;

namespace Indice.AspNetCore.Identity.Data.Models
{
    /// <summary>User devices representation.</summary>
    public class UserDevice
    {
        /// <summary>Constructs a new instance of <see cref="UserDevice"/> with a new <see cref="Guid"/> as Id.</summary>
        public UserDevice() : this(Guid.NewGuid()) { }

        /// <summary>Constructs a new instance of <see cref="UserDevice"/> using the given <see cref="Guid"/> as Id.</summary>
        /// <param name="id">The primary key.</param>
        public UserDevice(Guid id) => Id = id;

        /// <summary>The primary key.</summary>
        public Guid Id { get; }
        /// <summary>Device id.</summary>
        public string DeviceId { get; set; }
        /// <summary>The user id related to this device.</summary>
        public string UserId { get; set; }
        /// <summary>Device operating system.</summary>
        public DevicePlatform Platform { get; set; }
        /// <summary>Device name.</summary>
        public string Name { get; set; }
        /// <summary>Device model.</summary>
        public string Model { get; set; }
        /// <summary>Device OS version.</summary>
        public string OsVersion { get; set; }
        /// <summary>The date this device was created.</summary>
        public DateTimeOffset DateCreated { get; set; }
        /// <summary>Gets or sets the date and time, in UTC, when the device last signed in.</summary>
        public DateTimeOffset? LastSignInDate { get; set; }
        /// <summary>Flag that determines if push notifications are enabled for this device.</summary>
        public bool IsPushNotificationsEnabled { get; set; }
        /// <summary>Associated password for device (when <see cref="InteractionMode"/> is equal to <see cref="InteractionMode.Pin"/>).</summary>
        public string Password { get; set; }
        /// <summary>Flag for pin support.</summary>
        public bool SupportsPinLogin => !string.IsNullOrWhiteSpace(Password);
        /// <summary>Device public key (when <see cref="InteractionMode"/> is equal to <see cref="InteractionMode.Fingerprint"/>).</summary>
        public string PublicKey { get; set; }
        /// <summary>Flag for fingerprint support.</summary>
        public bool SupportsFingerprintLogin => !string.IsNullOrWhiteSpace(PublicKey);
        /// <summary>Extra metadata for the device.</summary>
        public dynamic Data { get; set; }
        /// <summary>Platform notification service handle.</summary>
        public string PnsHandle { get; set; }
        /// <summary>Device tags</summary>
        public string[] Tags { get; set; }
        /// <summary>Indicates whether the device is blocked.</summary>
        public bool RequiresPassword { get; set; }
        /// <summary>Indicates whether the device is a trusted device (i.e. capable of strong customer authentication scenarios).</summary>
        public bool IsTrusted { get; set; }
        /// <summary>The date that the device can be activated for trust.</summary>
        public DateTimeOffset? TrustActivationDate { get; set; }
        /// <summary>Determines whether the device is pending trust activation.</summary>
        public bool IsPendingTrustActivation => TrustActivationDate.HasValue && TrustActivationDate.Value > DateTimeOffset.UtcNow;
        /// <summary>Indicates whether device is blocked for any action.</summary>
        public bool Blocked { get; set; }
        /// <summary>The user associated with this device.</summary>
        public virtual User User { get; set; }
    }

    /// <summary>Models the way a device interacts with the identity system for trusted authorization.</summary>
    public enum InteractionMode
    {
        /// <summary>Fingerprint</summary>
        Fingerprint,
        /// <summary>4-pin</summary>
        Pin
    }
}
