using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user's password is changed through <see cref="ExtendedUserManager{User}"/>.</summary>
/// <remarks>Creates a new instance of <see cref="PasswordSetEvent"/>.</remarks>
/// <param name="user">The user context.</param>
/// <param name="suppressNotification">Whether to suppress notification.</param>
public class PasswordSetEvent(UserEventContext user, bool suppressNotification) : IPlatformEvent
{
    /// <summary>The user context.</summary>
    public UserEventContext User { get; } = user;

    /// <summary>Whether to suppress notification.</summary>
    public bool SuppressNotification { get; } = suppressNotification;
}
