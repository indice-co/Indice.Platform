using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>
/// Represents a model for sending token-based emails, typically used for user authentication or account-related
/// actions.
/// </summary>
/// <remarks>This model encapsulates information required to generate and send an email containing a token for
/// user verification or other purposes. It includes user details, the token, and associated metadata such as the
/// confirmation URL and email subject.</remarks>
public class TokenBasedEmailModel
{

    /// <summary>The user instance.</summary>
    public User? User { get; set; }
    /// <summary>The username</summary>
    public string? UserName => User?.UserName;
    /// <summary>User's name for display purposes.</summary>
    public string? DisplayName { get; set; }
    /// <summary>The token created for the user.</summary>
    public string? Token { get; set; }
    /// <summary>The token confirmation url.</summary>
    public string? Url { get; set; }
    /// <summary>The email subject.</summary>
    public string? Subject { get; set; }
    /// <summary>The URL to return to.</summary>
    public string? ReturnUrl { get; set; }
    /// <summary>The recipient of the email</summary>
    public string? RecipientEmail => User?.Email;
}

/// <summary>Models the data being sent to the view template for email messages.</summary>
public class EmailChangeEmailModel : TokenBasedEmailModel
{
    /// <summary>The new email address that the user wants to confirm.</summary>
    public string? NewEmail { get; set; }
}
