using System.ComponentModel.DataAnnotations;

namespace ResourceOwnerPasswordFlow.Models;

public class LoginViewModel
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Please enter an email")]
    public string Email { get; set; }
    [Required(AllowEmptyStrings = false, ErrorMessage = "Please enter a password")]
    public string Password { get; set; }
    public bool RememberMe { get; set; }
    public string ReturnUrl { get; set; }
}
