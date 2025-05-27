using Indice.Services;

namespace Indice.Features.Identity.Server.Options;

/// <summary>Options for the email sent when a user updates his email address.</summary>
public class EmailOptions
{
    /// <summary>Controls whether an email is sent to the user when the email is updated. Defaults to false.</summary>
    /// <remarks>Hint: also remember to register an implementation of <see cref="IEmailService"/>.</remarks>
    public bool SendEmailOnUpdate { get; set; } = false;
    /// <summary>The optional template to use for the email update email message. Default is <strong>EmailConfirmYourEmail</strong>.</summary>
    public string UpdateEmailTemplate { get; set; } = "EmailConfirmYourEmail";
    /// <summary>The optional template to use for the email change email message. Default is <strong>EmailConfirmEmailChange</strong>.</summary>
    public string ChangeEmailTemplate { get; set; } = "EmailConfirmEmailChange";
    /// <summary>The template to use for the password update email message. Default is <strong>EmailForgotPassword</strong>.</summary>
    public string ForgotPasswordTemplate { get; set; } = "EmailForgotPassword";
}
