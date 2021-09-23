using Indice.AspNetCore.Identity.Api.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Api.Events
{
    /// <summary>
    /// An event that is raised when a new device is created on IdentityServer.
    /// </summary>
    public class DeviceCreatedEvent : IIdentityServerApiEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="DeviceCreatedEvent"/>.
        /// </summary>
        /// <param name="device">The device created.</param>
        /// <param name="user">Related user.</param>
        public DeviceCreatedEvent(DeviceInfo device, SingleUserInfo user) {
            Device = device;
            User = user;
        }

        /// <summary>
        /// The device created.
        /// </summary>
        public DeviceInfo Device { get; }
        /// <summary>
        /// Related user.
        /// </summary>
        public SingleUserInfo User { get; }
    }
}
