using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user's username is changed on through <see cref="ExtendedUserManager{TUser}"/>.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
public class UserNameChangedEvent<TUser> : IPlatformEvent where TUser : DbUser
{
    /// <summary>Creates a new instance of <see cref="UserNameChangedEvent{TUser}"/>.</summary>
    /// <param name="user">The user entity.</param>
    public UserNameChangedEvent(TUser user) => User = user;

    /// <summary>The user entity.</summary>
    public TUser User { get; }
}

/// <summary>An event that is raised when a user's username is changed on through <see cref="ExtendedUserManager{TUser}"/>.</summary>
public class UserNameChangedEvent : UserNameChangedEvent<DbUser>
{
    /// <summary>Creates a new instance of <see cref="UserNameChangedEvent"/>.</summary>
    public UserNameChangedEvent(DbUser user) : base(user) { }
}
