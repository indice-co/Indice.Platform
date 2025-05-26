using System.ComponentModel.DataAnnotations;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models the request of a user for email confirmation.</summary>
public class ConfirmEmailChangeRequest
{
    /// <summary>
    /// The new email address that the user wants to confirm.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string Email { get; set; } = null!;
    /// <summary>The token. </summary>
    [Required(AllowEmptyStrings = false)]
    public string Token { get; set; } = null!;
}
