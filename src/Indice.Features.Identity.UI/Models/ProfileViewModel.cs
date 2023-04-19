using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.UI.Models;

/// <summary>Manage profile page view model.</summary>
public class ProfileViewModel : ProfileInputModel
{
    /// <summary></summary>
    public bool EmailChangePending { get; set; }
    /// <summary></summary>
    public IList<UserLoginInfo> CurrentLogins { get; set; }
    /// <summary></summary>
    public IList<AuthenticationScheme> OtherLogins { get; set; }
    /// <summary></summary>
    public bool CanRemoveProvider { get; set; }
    /// <summary></summary>
    public bool HasDeveloperTotp { get; set; }
}
