using System.ComponentModel.DataAnnotations;

namespace Indice.Features.Identity.Server.Mfa.Models;

/// <summary></summary>
public class RejectMfaLoginModel
{
    /// <summary></summary>
    [Required]
    public string? ConnectionId { get; set; }
}
