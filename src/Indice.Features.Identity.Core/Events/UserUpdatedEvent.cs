using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a new user is created through <see cref="ExtendedUserManager{User}"/>.</summary>
/// <remarks>Creates a new instance of <see cref="UserUpdatedEvent"/>.</remarks>
/// <param name="oldUser">The old user entity.</param>
/// <param name="newUser">The new user entity.</param>
public class UserUpdatedEvent(UserEventContext oldUser, UserEventContext newUser) : IPlatformEvent
{
    /// <summary>The old user entity.</summary>
    public UserEventContext OldUser { get; } = oldUser;
    /// <summary>The new user entity.</summary>
    public UserEventContext NewUser { get; } = newUser;
}