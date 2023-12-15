using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a device requests trust.</summary>
public class DeviceTrustRequestedEvent : IPlatformEvent
{
    /// <summary>Creates a new instance of <see cref="DeviceTrustRequestedEvent"/>.</summary>
    /// <param name="device">The device context.</param>
    /// <param name="user">The user context.</param>
    public DeviceTrustRequestedEvent(UserDeviceEventContext device, UserEventContext user) {
        Device = device;
        User = user;
    }

    /// <summary>The device context.</summary>
    public UserDeviceEventContext Device { get; }
    /// <summary>The user context.</summary>
    public UserEventContext User { get; }
}
