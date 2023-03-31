using System.ComponentModel.DataAnnotations;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models the request of a user for email confirmation.</summary>
public class ConfirmEmailRequest
{
    /// <summary>The token. </summary>
    [Required(AllowEmptyStrings = false)]
    public string Token { get; set; }
}
