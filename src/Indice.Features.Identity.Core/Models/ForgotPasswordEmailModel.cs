using Indice.Features.Identity.Core.Data.Models;
namespace Indice.Features.Identity.Core.Models;

/// <summary>
/// Represents the data model used for constructing an email sent to a user requesting a password reset.
/// </summary>
public class ForgotPasswordEmailModel
{
    /// <summary>
    /// Gets or sets the display name of the user to greet in the email. In case their name is not found this will default to the <see cref="User.UserName"/>
    /// </summary>
    public required string UserName { get; set; }

    /// <summary>
    /// Gets or sets the password reset callback URL.
    /// </summary>
    public required string Url { get; set; }
}
