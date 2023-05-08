namespace Indice.Features.Identity.UI.Models;

/// <summary>The input model that backs the add email page.</summary>
public class AddEmailInputModel
{
    /// <summary>The email address.</summary>
    public string? Email { get; set; }
    /// <summary>The return URL.</summary>
    public string? ReturnUrl { get; set; }
}
