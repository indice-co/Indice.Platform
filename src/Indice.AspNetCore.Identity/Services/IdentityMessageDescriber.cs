namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// Provides an extensibility point for altering localizing used inside the package.
    /// </summary>
    public class IdentityMessageDescriber
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
        public virtual string UpdateEmailMessageSubject => string.Format(Resources.Culture, Resources.EmailUpdateMessageSubject);
        /// <summary>
        /// Email verification code is {0}.
        /// </summary>
        public virtual string UpdateEmailMessageBody<TUser>(TUser user, string token, string returnUrl) where TUser : class => string.Format(Resources.Culture, Resources.EmailUpdateMessageBody, token);
        /// <summary>
        /// Forgot password.
        /// </summary>
        public virtual string ForgotPasswordMessageSubject => string.Format(Resources.Culture, Resources.ForgotPasswordMessageSubject);
        /// <summary>
        /// Email verification code is {0}.
        /// </summary>
        public virtual string ForgotPasswordMessageBody<TUser>(TUser user, string token) where TUser : class => string.Format(Resources.Culture, Resources.ForgotPasswordMessageBody, token);
        /// <summary>
        /// Your password is very common to use.
        /// </summary>
        public virtual string PasswordIsCommon => string.Format(Resources.Culture, Resources.PasswordIsCommon);
        /// <summary>
        /// Your password is identical to your username.
        /// </summary>
        public virtual string PasswordIdenticalToUserName => string.Format(Resources.Culture, Resources.PasswordIdenticalToUserName);
        /// <summary>
        /// This password has been used recently.
        /// </summary>
        public virtual string PasswordRecentlyUsed => string.Format(Resources.Culture, Resources.PasswordRecentlyUsed);
        /// <summary>
        /// Password cannot contain non latin characters.
        /// </summary>
        public virtual string PasswordHasNonLatinChars => string.Format(Resources.Culture, Resources.PasswordHasNonLatinChars);
        /// <summary>
        /// Your password is very easy to guess, please choose a more complex one.
        /// </summary>
        public virtual string PasswordIsCommonRequirement => string.Format(Resources.Culture, Resources.PasswordIsCommonRequirement);
        /// <summary>
        /// Your password looks a lot like your username which can lead to your account been hacked.
        /// </summary>
        public virtual string PasswordIdenticalToUserNameRequirement => string.Format(Resources.Culture, Resources.PasswordIdenticalToUserNameRequirement);
        /// <summary>
        /// It is a good practise not to re-use your past password.
        /// </summary>
        public virtual string PasswordRecentlyUsedRequirement => string.Format(Resources.Culture, Resources.PasswordRecentlyUsedRequirement);
        /// <summary>
        /// Your password cannot contain non-Latin characters, which is required by the password policy.
        /// </summary>
        public virtual string PasswordHasNonLatinCharsRequirement => string.Format(Resources.Culture, Resources.PasswordHasNonLatinCharsRequirement);
    }
}
