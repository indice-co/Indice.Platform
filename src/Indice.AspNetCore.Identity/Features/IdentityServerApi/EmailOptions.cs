using Indice.Services;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Options for the email sent when a user updates his email address.
    /// </summary>
    public class EmailOptions
    {
        /// <summary>
        /// Controls whether an email is sent to the user when the email is updated, containing a verification token, or not. Defaults to false.
        /// </summary>
        /// <remarks>Hint: also remember to register an implementation of <see cref="IEmailService"/>.</remarks>
        public bool SendEmailOnUpdate { get; set; } = false;
        /// <summary>
        /// The optional template (Razor view) to use for the email message.
        /// </summary>
        public string TemplateName { get; set; } = "Email";
    }
}
