using System.ComponentModel.DataAnnotations;
using Indice.Features.Identity.Core.Data.Models;

namespace Indice.AspNetCore.Identity.Api.Models;

/// <summary>Models the request to update the password expiration policy for the user.</summary>
public class UpdatePasswordExpirationPolicyRequest
{
    /// <summary>The policy to apply for password expiration.</summary>
    [Required]
    public PasswordExpirationPolicy Policy { get; set; }
}
