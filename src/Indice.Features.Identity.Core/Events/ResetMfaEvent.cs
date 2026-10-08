using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user's reset MFA is invoked from an admin user</summary>
/// <remarks>Creates a new instance of <see cref="ResetMfaEvent"/>.</remarks>
/// <param name="user">The user context.</param>
public class ResetMfaEvent(UserEventContext user) : IPlatformEvent
{
    /// <summary>The user context.</summary>
    public UserEventContext User { get; } = user;
}