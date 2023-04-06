namespace Indice.Features.Identity.UI.Models;

/// <summary>Input model for the logout page.</summary>
public class LogoutInputModelTemp
{
    /// <summary>The logout id.</summary>
    public string LogoutId { get; set; }
    /// <summary>The id of the current client in the request. </summary>
    public string ClientId { get; set; }
}
