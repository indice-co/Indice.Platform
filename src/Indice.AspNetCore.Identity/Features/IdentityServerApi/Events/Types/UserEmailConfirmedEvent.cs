namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// An event that is raised when a user's email is confirmed.
    /// </summary>
    public class UserEmailConfirmedEvent : IIdentityServerApiEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserEmailConfirmedEvent"/>.
        /// </summary>
        /// <param name="user">The instance of the user that confirmed the phone number.</param>
        public UserEmailConfirmedEvent(BasicUserInfo user) => User = user;

        /// <summary>
        /// The instance of the user that confirmed the email.
        /// </summary>
        public BasicUserInfo User { get; private set; }
    }
}
