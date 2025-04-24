using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.UI.Models;

/// <summary>MFA login view model.</summary>
public class MfaLoginViewModel<TUser> : MfaLoginInputModel where TUser : User, new()
{
    /// <summary>The user entity.</summary>
    public TUser User { get; set; } = new TUser();
    /// <summary>Allows to choose less secure authentication method for MFA, if possible.</summary>
    public bool AllowDowngradeAuthenticationMethod { get; set; }
    /// <summary>The authentication method that will be used for MFA.</summary>
    public AuthenticationMethod? AuthenticationMethod { get; set; }
    /// <summary>Indicates whether the current browser device already exists for the user.</summary>
    public bool IsExistingBrowser { get; set; }
    /// <summary>The error message that informs the user that mfa cannot be performed.</summary>
    public string? Error { get; set; }
    /// <summary>True if the error message is populated. This is an indicator that the mfa process is in deadlock.</summary>
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
}

/// <summary>MFA login view model.</summary>
public class MfaLoginViewModel : MfaLoginViewModel<User> { }
