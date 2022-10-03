using Indice.AspNetCore.Identity.Data.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Events
{
    /// <summary>An event that is raised when a new user is created through <see cref="ExtendedUserManager{TUser}"/>.</summary>
    /// <typeparam name="TUser">The type of user.</typeparam>
    public class UserCreatedEvent<TUser> : IPlatformEvent where TUser : User
    {
        /// <summary>Creates a new instance of <see cref="UserCreatedEvent{TUser}"/>.</summary>
        /// <param name="user">The user entity.</param>
        public UserCreatedEvent(TUser user) => User = user;

        /// <summary>The user entity.</summary>
        public TUser User { get; }
    }

    /// <summary>An event that is raised when a new user is created through <see cref="ExtendedUserManager{TUser}"/>.</summary>
    public class UserCreatedEvent : UserCreatedEvent<User>
    {
        /// <summary>Creates a new instance of <see cref="UserCreatedEvent"/>.</summary>
        public UserCreatedEvent(User user) : base(user) { }
    }
}
