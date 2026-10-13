using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a new user is created through <see cref="ExtendedUserManager{User}"/>.</summary>
/// <remarks>Creates a new instance of <see cref="UserUpdatedEvent"/>.</remarks>
/// <param name="PreviousUser">The previous snapshot of user entity.</param>
/// <param name="User">The user entity.</param>
public class UserUpdatedEvent(UserEventContext PreviousUser, UserEventContext User) : IPlatformEvent
{
    /// <summary>The previous snapshot of user entity.</summary>
    public UserEventContext PreviousUser { get; } = PreviousUser;
    /// <summary>The user entity.</summary>
    public UserEventContext User { get; } = User;
}