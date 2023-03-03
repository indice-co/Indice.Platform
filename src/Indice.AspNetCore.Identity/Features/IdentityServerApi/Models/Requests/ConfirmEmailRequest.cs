using System.ComponentModel.DataAnnotations;

namespace Indice.AspNetCore.Identity.Api.Models;

/// <summary>Models the request of a user for email confirmation.</summary>
public class ConfirmEmailRequest
{
    /// <summary>The token. </summary>
    [Required(AllowEmptyStrings = false)]
    public string Token { get; set; }
}
