using System.ComponentModel.DataAnnotations;

namespace Indice.Identity.Models;

public class ApproveLoginRequest
{
    [Required]
    public string ConnectionId { get; set; }

    [Required]
    public string Otp { get; set; }
}
