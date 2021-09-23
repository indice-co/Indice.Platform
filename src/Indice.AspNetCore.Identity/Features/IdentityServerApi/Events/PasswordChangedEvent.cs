using Indice.AspNetCore.Identity.Api.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Api.Events
{
    /// <summary>
    /// An event that is raised when a user's password is changed on IdentityServer.
    /// </summary>
    public class PasswordChangedEvent : IIdentityServerApiEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="PasswordChangedEvent"/>.
        /// </summary>
        /// <param name="user">Related user.</param>
        public PasswordChangedEvent(SingleUserInfo user) {
            User = user;
        }

        /// <summary>
        /// Related user.
        /// </summary>
        public SingleUserInfo User { get; set; }
    }
}
