using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user's password is changed through <see cref="ExtendedUserManager{TUser}"/>.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
public class PasswordChangedEvent<TUser> : IPlatformEvent where TUser : DbUser
{
    /// <summary>Creates a new instance of <see cref="PasswordChangedEvent{TUser}"/>.</summary>
    /// <param name="user">The user entity.</param>
    public PasswordChangedEvent(TUser user) => User = user;

    /// <summary>The user entity.</summary>
    public TUser User { get; }
}

/// <summary>An event that is raised when a user's password is changed on IdentityServer.</summary>
public class PasswordChangedEvent : PasswordChangedEvent<DbUser>
{
    /// <summary>Creates a new instance of <see cref="PasswordChangedEvent"/>.</summary>
    /// <param name="user">The user entity.</param>
    public PasswordChangedEvent(DbUser user) : base(user) { }
}
