using Indice.Services;

namespace Indice.AspNetCore.Identity.Api.Configuration;

/// <summary>Options for the email sent when a user updates his email address.</summary>
public class EmailOptions
{
    /// <summary>Controls whether an email is sent to the user when the email is updated. Defaults to false.</summary>
    /// <remarks>Hint: also remember to register an implementation of <see cref="IEmailService"/>.</remarks>
    public bool SendEmailOnUpdate { get; set; } = false;
    /// <summary>The optional template to use for the email change email message. Default is null.</summary>
    public string UpdateEmailTemplate { get; set; }
    /// <summary>The template to use for the password update email message. Default is null.</summary>
    public string ForgotPasswordTemplate { get; set; }
}
