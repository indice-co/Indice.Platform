using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a device is updated through <see cref="ExtendedUserManager{User}"/>.</summary>
/// <remarks>Creates a new instance of <see cref="DeviceUpdatedEvent"/>.</remarks>
/// <param name="device">The device context.</param>
/// <param name="user">The user context.</param>
public class DeviceUpdatedEvent(UserDeviceEventContext device, UserEventContext user) : IPlatformEvent
{
    /// <summary>The device context.</summary>
    public UserDeviceEventContext Device { get; } = device;
    /// <summary>The user context.</summary>
    public UserEventContext User { get; } = user;
}
