using Indice.AspNetCore.Identity.Api.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Api.Events
{
    /// <summary>
    /// An event that is raised when a new user is created (by an admin) using IdentityServer API.
    /// </summary>
    public class UserCreatedEvent : IIdentityServerApiEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserCreatedEvent"/>.
        /// </summary>
        /// <param name="user"></param>
        public UserCreatedEvent(SingleUserInfo user) => User = user;

        /// <summary>
        /// The instance of the user that was created.
        /// </summary>
        public SingleUserInfo User { get; private set; }
    }
}
