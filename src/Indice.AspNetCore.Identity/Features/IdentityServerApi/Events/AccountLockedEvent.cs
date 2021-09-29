using Indice.AspNetCore.Identity.Api.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Api.Events
{
    /// <summary>
    /// An event that is raised when an account is locked on IdentityServer.
    /// </summary>
    public class AccountLockedEvent : IIdentityServerApiEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="AccountLockedEvent"/>.
        /// </summary>
        /// <param name="user">Related user.</param>
        public AccountLockedEvent(SingleUserInfo user) {
            User = user;
        }

        /// <summary>
        /// Related user.
        /// </summary>
        public SingleUserInfo User { get; }
    }
}
