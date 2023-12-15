using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a new user is created through <see cref="ExtendedUserManager{User}"/>.</summary>
/// <remarks>Creates a new instance of <see cref="UserCreatedEvent"/>.</remarks>
/// <param name="user">The user entity.</param>
public class UserCreatedEvent(UserEventContext user) : IPlatformEvent
{
    /// <summary>The user entity.</summary>
    public UserEventContext User { get; } = user;
}
