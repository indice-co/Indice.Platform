using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user is deleted form the system.</summary>
/// <remarks>Creates a new instance of <see cref="UserBlockedEvent"/>.</remarks>
/// <param name="user">The user context.</param>
public class UserDeletedEvent(UserEventContext user) : IPlatformEvent
{
    /// <summary>The user context.</summary>
    public UserEventContext User { get; } = user;
}
