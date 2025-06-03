using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models the data being sent to the view template for email messages.</summary>
public class EmailChangeEmailModel
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
    /// <summary>The new email address that the user wants to confirm.</summary>
    public string? NewEmail { get; set; }
}
