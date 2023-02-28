using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user's email is confirmed through <see cref="ExtendedUserManager{TUser}"/>.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
public class EmailConfirmedEvent<TUser> : IPlatformEvent where TUser : DbUser
{
    /// <summary>Creates a new instance of <see cref="EmailConfirmedEvent{TUser}"/>.</summary>
    /// <param name="user">The user entity.</param>
    public EmailConfirmedEvent(TUser user) => User = user;

    /// <summary>The user entity.</summary>
    public TUser User { get; }
}

/// <summary>An event that is raised when a user's email is confirmed, through <see cref="ExtendedUserManager{TUser}"/>.</summary>
public class EmailConfirmedEvent : EmailConfirmedEvent<DbUser>
{
    /// <summary>Creates a new instance of <see cref="EmailConfirmedEvent{TUser}"/>.</summary>
    public EmailConfirmedEvent(DbUser user) : base(user) { }
}
