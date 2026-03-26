namespace Indice.Features.Identity.Core.Models;

/// <summary>
/// Represents the data model used for constructing an email sent to a user requesting a password reset.
/// </summary>
public class ForgotPasswordEmailModel
{
    /// <summary>
    /// Gets or sets the display name of the user to greet in the email.
    /// </summary>
    public required string UserName { get; set; }

    /// <summary>
    /// Gets or sets the password reset callback URL.
    /// </summary>
    public required string Url { get; set; }
}
