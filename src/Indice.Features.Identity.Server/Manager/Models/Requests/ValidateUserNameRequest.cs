using System.ComponentModel.DataAnnotations;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Class that models the request for validating a user's username.</summary>
public class ValidateUserNameRequest
{
    /// <summary>The username.</summary>
    [Required]
    public string UserName { get; set; }
}
