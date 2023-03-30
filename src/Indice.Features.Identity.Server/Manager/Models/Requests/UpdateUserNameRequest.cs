using System.ComponentModel.DataAnnotations;

namespace Indice.Features.Identity.Server.Manager.Models.Requests;

/// <summary>Models a request for changing the username.</summary>
public class UpdateUserNameRequest
{
    /// <summary>The new username.</summary>
    [Required]
    public string UserName { get; set; }
}
