using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a new device is created through <see cref="ExtendedUserManager{TUser}"/>.</summary>
/// <typeparam name="TDevice">The type of the device.</typeparam>
/// <typeparam name="TUser">The type of the user.</typeparam>
public class DeviceCreatedEvent<TDevice, TUser> : IPlatformEvent
    where TDevice : UserDevice
    where TUser : User
{
    /// <summary>Creates a new instance of <see cref="DeviceCreatedEvent{TDevice, TUser}"/>.</summary>
    /// <param name="device">The device entity.</param>
    /// <param name="user">The user entity.</param>
    public DeviceCreatedEvent(TDevice device, TUser user) {
        Device = device;
        User = user;
    }

    /// <summary>The device entity.</summary>
    public TDevice Device { get; }
    /// <summary>The user entity.</summary>
    public TUser User { get; }
}

/// <summary>An event that is raised when a new device is created through <see cref="ExtendedUserManager{TUser}"/>.</summary>
public class DeviceCreatedEvent : DeviceCreatedEvent<UserDevice, User>
{
    /// <summary>Creates a new instance of <see cref="DeviceCreatedEvent"/>.</summary>
    /// <param name="device">The device entity.</param>
    /// <param name="user">The user entity.</param>
    public DeviceCreatedEvent(UserDevice device, User user) : base(device, user) { }
}
