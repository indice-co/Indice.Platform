using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user's phone number is confirmed through <see cref="ExtendedUserManager{User}"/>.</summary>
/// <remarks>Creates a new instance of <see cref="PhoneNumberConfirmedEvent"/>.</remarks>
/// <param name="user">The user context.</param>
public class PhoneNumberConfirmedEvent(UserEventContext user) : IPlatformEvent
{
    /// <summary>The user context.</summary>
    public UserEventContext User { get; } = user;
}
