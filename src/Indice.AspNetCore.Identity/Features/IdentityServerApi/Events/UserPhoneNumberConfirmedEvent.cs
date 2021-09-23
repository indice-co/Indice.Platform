using Indice.AspNetCore.Identity.Api.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Api.Events
{
    /// <summary>
    /// An event that is raised when a user's phone number is confirmed.
    /// </summary>
    public class UserPhoneNumberConfirmedEvent : IIdentityServerApiEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserPhoneNumberConfirmedEvent"/>.
        /// </summary>
        /// <param name="user">The instance of the user that confirmed the phone number.</param>
        public UserPhoneNumberConfirmedEvent(BasicUserInfo user) => User = user;

        /// <summary>
        /// The instance of the user that confirmed the phone number.
        /// </summary>
        public BasicUserInfo User { get; }
    }
}
