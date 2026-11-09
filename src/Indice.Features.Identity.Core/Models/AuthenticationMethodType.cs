namespace Indice.Features.Identity.Core.Models;

/// <summary>An enumeration type for the <see cref="AuthenticationMethod"/>.</summary>
public enum AuthenticationMethodType
{
    /// <summary>Phone number</summary>
    PhoneNumber,
    /// <summary>FIDO2</summary>
    Fido2,
    /// <summary>Authenticator app</summary>
    AuthenticatorApp,
    /// <summary>Trusted Device, push otp</summary>
    TrustedDevice,
    /// <summary>Email</summary>
    Email,
    /// <summary>Recovery code</summary>
    RecoveryCode
}
