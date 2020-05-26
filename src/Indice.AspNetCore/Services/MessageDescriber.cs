using Indice.AspNetCore;

namespace Indice.Services
{
    /// <summary>
    /// Provides the various messages used throughout Indice packages.
    /// </summary>
    public class MessageDescriber
    {
        /// <summary>
        /// User already has email '0'.
        /// </summary>
        /// <param name="email">The email address.</param>
        public virtual string EmailAlreadyExists(string email) => string.Format(Resources.Culture, Resources.UserAlreadyHasEmail, email);
        /// <summary>
        /// User's email is already confirmed.
        /// </summary>
        public virtual string EmailAlreadyConfirmed => string.Format(Resources.Culture, Resources.EmailAlreadyConfirmed);
        /// <summary>
        /// User already has phone number '{0}'.
        /// </summary>
        /// <param name="phoneNumber">The phone number.</param>
        public virtual string UserAlreadyHasPhoneNumber(string phoneNumber) => string.Format(Resources.Culture, Resources.UserAlreadyHasPhoneNumber, phoneNumber);
        /// <summary>
        /// User's phone number is already confirmed.
        /// </summary>
        public virtual string PhoneNumberAlreadyConfirmed => string.Format(Resources.Culture, Resources.PhoneNumberAlreadyConfirmed);
        /// <summary>
        /// SMS verification code is {token}.
        /// </summary>
        /// <param name="token">Phone number verification token.</param>
        public virtual string PhoneNumberVerificationMessage(string token) => string.Format(Resources.Culture, Resources.PhoneNumberVerificationMessage, token);
        /// <summary>
        /// Confirm your account.
        /// </summary>
        public virtual string EmailUpdateMessageSubject => string.Format(Resources.Culture, Resources.EmailUpdateMessageSubject);
        /// <summary>
        /// Email verification code is {0}.
        /// </summary>
        public virtual string EmailUpdateMessageBody(string userId, string token) => string.Format(Resources.Culture, Resources.EmailUpdateMessageBody);
        /// <summary>
        /// Your password is very common to use.
        /// </summary>
        public virtual string PasswordIsCommon() => string.Format(Resources.Culture, Resources.PasswordIsCommon);
        /// <summary>
        /// Your password is identical to your username.
        /// </summary>
        public virtual string PasswordIdenticalToUserName() => string.Format(Resources.Culture, Resources.PasswordIdenticalToUserName);
        /// <summary>
        /// This password has been used recently.
        /// </summary>
        public virtual string PasswordRecentlyUsed() => string.Format(Resources.Culture, Resources.PasswordRecentlyUsed);
        /// <summary>
        /// Password cannot contain non latin characters.
        /// </summary>
        public virtual string PasswordHasNonLatinChars() => string.Format(Resources.Culture, Resources.PasswordHasNonLatinChars);
    }
}
