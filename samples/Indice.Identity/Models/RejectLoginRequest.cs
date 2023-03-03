using System.ComponentModel.DataAnnotations;

namespace Indice.Identity.Models;

public class RejectLoginRequest
{
    [Required]
    public string ConnectionId { get; set; }
}
