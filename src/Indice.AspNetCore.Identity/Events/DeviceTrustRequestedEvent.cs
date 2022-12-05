using Indice.AspNetCore.Identity.Data.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Events
{
    /// <summary>An event that is raised when a device requests trust.</summary>
    /// <typeparam name="TDevice">The type of the device.</typeparam>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    public class DeviceTrustRequestedEvent<TDevice, TUser> : IPlatformEvent
        where TDevice : UserDevice
        where TUser : User
    {
        /// <summary>Creates a new instance of <see cref="DeviceTrustRequestedEvent{TDevice, TUser}"/>.</summary>
        /// <param name="device">The device entity.</param>
        /// <param name="user">The user entity.</param>
        public DeviceTrustRequestedEvent(TDevice device, TUser user) {
            Device = device;
            User = user;
        }

        /// <summary>The device entity.</summary>
        public TDevice Device { get; }
        /// <summary>The user entity.</summary>
        public TUser User { get; }
    }

    /// <summary>An event that is raised when a device requests trust.</summary>
    public class DeviceTrustRequestedEvent : DeviceTrustRequestedEvent<UserDevice, User>
    {
        /// <summary>Creates a new instance of <see cref="DeviceTrustRequestedEvent"/>.</summary>
        /// <param name="device">The device entity.</param>
        /// <param name="user">The user entity.</param>
        public DeviceTrustRequestedEvent(UserDevice device, User user) : base(device, user) { }
    }
}
