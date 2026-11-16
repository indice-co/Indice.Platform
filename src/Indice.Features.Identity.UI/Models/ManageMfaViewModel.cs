using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.UI.Models;

/// <summary>View model for the two-factor authentication management page.</summary>
public class ManageMfaViewModel
{
    /// <summary>Indicates whether two-factor authentication is enabled for the user.</summary>
    public bool TwoFactorEnabled { get; set; }
    /// <summary>The method that is currently used to sign in, when two-factor authentication is enabled.</summary>
    public ManageMfaMethodViewModel? CurrentMethod => Methods.FirstOrDefault(method => method.IsCurrent);
    /// <summary>The methods the user can choose from.</summary>
    public IList<ManageMfaMethodViewModel> Methods { get; set; } = [];
    /// <summary>Indicates whether the user can generate new recovery codes.</summary>
    public bool CanRegenerateRecoveryCodes { get; set; }
}

/// <summary>A two-factor authentication method on the management page.</summary>
public class ManageMfaMethodViewModel
{
    /// <summary>The method type.</summary>
    public AuthenticationMethodType Type { get; set; }
    /// <summary>The method code (see <see cref="AuthenticationMethod.Code"/>).</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>The display name of the method (not localized).</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>The detail of the method, for example the phone number or the email address the code is sent to.</summary>
    public string? Detail { get; set; }
    /// <summary>Indicates whether this is the method currently used to sign in.</summary>
    public bool IsCurrent { get; set; }
    /// <summary>Indicates whether the user can select the method. Phone and email methods require a verified phone number or email address.</summary>
    public bool IsAvailable { get; set; }
}

/// <summary>View model for the authenticator app setup step of the two-factor authentication management page.</summary>
public class ManageMfaSetupAuthenticatorViewModel : SetupAuthenticatorViewModel
{
    /// <summary>The protected pending authenticator key. It is stored for the user only after a valid code is entered.</summary>
    public string? ProtectedKey { get; set; }
}
