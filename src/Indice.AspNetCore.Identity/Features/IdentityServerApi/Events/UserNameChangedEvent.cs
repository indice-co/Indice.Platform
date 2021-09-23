using Indice.AspNetCore.Identity.Api.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Api.Events
{
    /// <summary>
    /// An event that is raised when a user's username is changed on IdentityServer.
    /// </summary>
    public class UserNameChangedEvent : IIdentityServerApiEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserNameChangedEvent"/>.
        /// </summary>
        /// <param name="user">Related user.</param>
        public UserNameChangedEvent(SingleUserInfo user) {
            User = user;
        }

        /// <summary>
        /// Related user.
        /// </summary>
        public SingleUserInfo User { get; }
    }
}
