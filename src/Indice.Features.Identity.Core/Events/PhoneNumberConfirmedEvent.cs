using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user's phone number is confirmed through <see cref="ExtendedUserManager{TUser}"/>.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
public class PhoneNumberConfirmedEvent<TUser> : IPlatformEvent where TUser : User
{
    /// <summary>Creates a new instance of <see cref="PhoneNumberConfirmedEvent{TUser}"/>.</summary>
    /// <param name="user">The user entity.</param>
    public PhoneNumberConfirmedEvent(TUser user) => User = user;

    /// <summary>The user entity.</summary>
    public TUser User { get; }
}

/// <summary>An event that is raised when a user's phone number is confirmed through <see cref="ExtendedUserManager{TUser}"/>.</summary>
public class PhoneNumberConfirmedEvent : PhoneNumberConfirmedEvent<User>
{
    /// <summary>Creates a new instance of <see cref="PhoneNumberConfirmedEvent"/>.</summary>
    public PhoneNumberConfirmedEvent(User user) : base(user) { }
}
