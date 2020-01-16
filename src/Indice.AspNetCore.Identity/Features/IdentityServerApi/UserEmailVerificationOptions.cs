using Indice.Services;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Options for the email sent to user for verification.
    /// </summary>
    public class UserEmailVerificationOptions
    {
        /// <summary>
        /// Specifies if an email verification is sent to the user upon creation. Default is true.
        /// </summary>
        /// <remarks>When this feature is enabled, an <see cref="IEmailService"/> must also be registered.</remarks>
        public bool Enabled { get; set; }
        /// <summary>
        /// The subject of the email verification email.
        /// </summary>
        public string Subject { get; set; } = "Confirm your account";
        /// <summary>
        /// The body of the email verification email.
        /// </summary>
        public string Body { get; set; } = @"Click <a href=""{callbackUrl}"">here</a> to verify your email.";
        /// <summary>
        /// The template to use for the email message.
        /// </summary>
        public string TemplateName { get; set; } = "Email";
        /// <summary>
        /// The URL to return after successful verification.
        /// </summary>
        public string ReturnUrl { get; set; }
    }
}
