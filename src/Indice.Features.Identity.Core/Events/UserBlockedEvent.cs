using Indice.Events;
using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user is blocked.</summary>
/// <typeparam name="TUser">The user type.</typeparam>
public class UserBlockedEvent<TUser> : IPlatformEvent where TUser : User
{
    /// <summary>Creates a new instance of <see cref="UserBlockedEvent{TUser}"/>.</summary>
    /// <param name="user">The user entity.</param>
    public UserBlockedEvent(TUser user) => User = user;

    /// <summary>The user entity.</summary>
    public TUser User { get; }
}

/// <summary>An event that is raised when a user is blocked.</summary>
public class UserBlockedEvent : UserBlockedEvent<User>
{
    /// <summary>Creates a new instance of <see cref="UserBlockedEvent"/>.</summary>
    public UserBlockedEvent(User user) : base(user) { }
}
