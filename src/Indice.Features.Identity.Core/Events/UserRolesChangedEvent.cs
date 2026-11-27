using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>The type of change to a user's role membership.</summary>
public enum UserRoleChangeType
{
    /// <summary>A role was added.</summary>
    Added,
    /// <summary>A role was removed.</summary>
    Removed
}

/// <summary>Raised after a user's role membership changes through <see cref="ExtendedUserManager{User}"/>.</summary>
/// <param name="user">The affected user.</param>
/// <param name="changeType">Whether the role was added or removed.</param>
/// <param name="role">The name of the affected role, or <see cref="GlobalAdminRole"/> for the global administrator flag.</param>
/// <param name="isGlobalAdmin">Whether this event represents a global administrator flag change rather than a role membership change.</param>
public class UserRolesChangedEvent(UserEventContext user, UserRoleChangeType changeType, string role, bool isGlobalAdmin = false) : IPlatformEvent
{
    /// <summary>Event role name used for changes to the global administrator flag; this is not an Identity role.</summary>
    public const string GlobalAdminRole = "GlobalAdmin";
    /// <summary>The affected user.</summary>
    public UserEventContext User { get; } = user;
    /// <summary>Whether the role was added or removed.</summary>
    public UserRoleChangeType ChangeType { get; } = changeType;
    /// <summary>The name of the affected role.</summary>
    public string Role { get; } = role;
    /// <summary>Whether the global administrator flag, rather than a role membership, changed.</summary>
    public bool IsGlobalAdmin { get; } = isGlobalAdmin;
}
