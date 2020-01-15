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
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public string Subject { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Body { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TemplateName { get; set; } = "Email";
    }
}
