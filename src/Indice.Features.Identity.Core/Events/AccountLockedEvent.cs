using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when an account is locked through <see cref="ExtendedUserManager{TUser}"/>.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
public class AccountLockedEvent<TUser> : IPlatformEvent where TUser : User
{
    /// <summary>Creates a new instance of <see cref="AccountLockedEvent{TKey}"/>.</summary>
    /// <param name="user">The user entity.</param>
    public AccountLockedEvent(TUser user) => User = user;

    /// <summary>The user entity.</summary>
    public TUser User { get; }
}

/// <summary>An event that is raised when an account is locked through <see cref="ExtendedUserManager{TUser}"/>.</summary>
public class AccountLockedEvent : AccountLockedEvent<User>
{
    /// <summary>Creates a new instance of <see cref="AccountLockedEvent"/>.</summary>
    /// <param name="user">The user entity.</param>
    public AccountLockedEvent(User user) : base(user) { }
}
