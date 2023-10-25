using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user's username is changed on through <see cref="ExtendedUserManager{TUser}"/>.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
public class UserNameChangedEvent<TUser> : IPlatformEvent where TUser : User
{
    /// <summary>Creates a new instance of <see cref="UserNameChangedEvent{TUser}"/>.</summary>
    /// <param name="user">The user entity.</param>
    /// <param name="previousValue">The previous value of the user's username.</param>
    public UserNameChangedEvent(TUser user, string previousValue) {
        User = user;
        PreviousValue = previousValue;
    }

    /// <summary>The user entity.</summary>
    public TUser User { get; }

    /// <summary>The previous state of the user.</summary>
    public string PreviousValue { get; }
}

/// <summary>An event that is raised when a user's username is changed on through <see cref="ExtendedUserManager{TUser}"/>.</summary>
public class UserNameChangedEvent : UserNameChangedEvent<User>
{
    /// <summary>Creates a new instance of <see cref="UserNameChangedEvent"/>.</summary>
    public UserNameChangedEvent(User user, string previousValue) : base(user, previousValue) { }
}
