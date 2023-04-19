namespace Indice.Features.Identity.UI.Models;

/// <summary>The view model that backs the external provider association page.</summary>
public class AssociateViewModel : AssociateInputModel
{
    /// <summary>If the user is an existing one then this is the user id to associate him with. </summary>
    /// <remarks>It is better to trust this than the username especially in scenarios where <see cref="AssociateInputModel.UserName"/> is the same as the <see cref="AssociateInputModel.Email"/>.</remarks>
    public string UserId { get; set; }
    /// <summary>The external id provider.</summary>
    public string Provider { get; set; }
}
