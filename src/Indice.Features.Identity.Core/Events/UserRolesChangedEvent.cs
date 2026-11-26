using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>Raised after a user's role membership changes through <see cref="ExtendedUserManager{User}"/>.</summary>
/// <param name="user">The affected user.</param>
/// <param name="addedRoles">The names of roles added to the user.</param>
/// <param name="removedRoles">The names of roles removed from the user.</param>
public class UserRolesChangedEvent(UserEventContext user, IEnumerable<string> addedRoles, IEnumerable<string> removedRoles) : IPlatformEvent
{
    /// <summary>The affected user.</summary>
    public UserEventContext User { get; } = user;
    /// <summary>The names of roles added to the user.</summary>
    public IReadOnlyList<string> AddedRoles { get; } = Array.AsReadOnly(addedRoles.ToArray());
    /// <summary>The names of roles removed from the user.</summary>
    public IReadOnlyList<string> RemovedRoles { get; } = Array.AsReadOnly(removedRoles.ToArray());
}
