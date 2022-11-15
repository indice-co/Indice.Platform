using Indice.AspNetCore.Identity.Data.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Events
{
    /// <summary>An event that is raised when a user's email is confirmed through <see cref="ExtendedUserManager{TUser}"/>.</summary>
    /// <typeparam name="TUser">The type of user.</typeparam>
    public class EmailConfirmedEvent<TUser> : IPlatformEvent where TUser : User
    {
        /// <summary>Creates a new instance of <see cref="EmailConfirmedEvent{TUser}"/>.</summary>
        /// <param name="user">The user entity.</param>
        public EmailConfirmedEvent(TUser user) => User = user;

        /// <summary>The user entity.</summary>
        public TUser User { get; }
    }

    /// <summary>An event that is raised when a user's email is confirmed, through <see cref="ExtendedUserManager{TUser}"/>.</summary>
    public class EmailConfirmedEvent : EmailConfirmedEvent<User>
    {
        /// <summary>Creates a new instance of <see cref="EmailConfirmedEvent{TUser}"/>.</summary>
        public EmailConfirmedEvent(User user) : base(user) { }
    }
}
