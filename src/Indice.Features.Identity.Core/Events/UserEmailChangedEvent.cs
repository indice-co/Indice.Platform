using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user's email is changed on through <see cref="ExtendedUserManager{User}"/>.</summary>
/// <remarks>Creates a new instance of <see cref="UserNameChangedEvent"/>.</remarks>
/// <param name="user">The user entity.</param>
/// <param name="previousValue">The previous value of the user's email.</param>
public class UserEmailChangedEvent(
    UserEventContext user, 
    string previousValue) : IPlatformEvent
{
    /// <summary>The user entity.</summary>
    public UserEventContext User { get; } = user;
    /// <summary>The previous state of the user email.</summary>
    public string PreviousValue { get; } = previousValue;
}
