using System.ComponentModel.DataAnnotations;

namespace Indice.Features.Identity.Server.Mfa.Models;

/// <summary></summary>
public class ApproveMfaLoginModel
{
    /// <summary></summary>
    [Required]
    public string? ConnectionId { get; set; }

    /// <summary></summary>
    [Required]
    public string? Otp { get; set; }
}
