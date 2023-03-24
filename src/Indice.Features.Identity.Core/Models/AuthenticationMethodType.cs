namespace Indice.Features.Identity.Core.Models;

/// <summary>An enumeration type for the <see cref="AuthenticationMethod"/>.</summary>
public enum AuthenticationMethodType
{
    /// <summary>SMS</summary>
    SMS,
    /// <summary>FIDO2</summary>
    Fido2,
    /// <summary>Microsoft Authenticator application</summary>
    MicrosoftAuthenticator,
    /// <summary>Biometrics</summary>
    Biometrics,
    /// <summary>Email</summary>
    Email
}
