using System;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Api.Events
{
    /// <summary>
    /// An event that is raised when a user's email confirmation is resend.
    /// </summary>
    public class UserEmailConfirmationResendEvent : IPlatformEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserEmailConfirmationResendEvent"/>.
        /// </summary>
        /// <param name="user">The instance of the user that email verification will be resend.</param>
        /// <param name="confirmationToken">The generated email confirmation token.</param>
        public UserEmailConfirmationResendEvent(SingleUserInfo user, string confirmationToken) {
            User = user ?? throw new ArgumentNullException(nameof(user));
            ConfirmationToken = confirmationToken ?? throw new ArgumentNullException(nameof(confirmationToken));
        }

        /// <summary>
        /// The instance of the user that email verification will be resend.
        /// </summary>
        public SingleUserInfo User { get; }
        /// <summary>
        /// The generated email confirmation token.
        /// </summary>
        public string ConfirmationToken { get; }
    }
}
