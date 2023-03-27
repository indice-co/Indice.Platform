namespace Indice.Features.Identity.Core.Models;

/// <summary>Describes the various authentication methods for multi-factor authentication.</summary>
public abstract class AuthenticationMethod
{
    /// <summary>Constructor blueprint for <see cref="AuthenticationMethod"/>.</summary>
    /// <param name="displayName">The name for the UI.</param>
    /// <param name="description">A detailed description.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    public AuthenticationMethod(string displayName, string description, bool enabled = true) {
        DisplayName = displayName;
        Description = description;
        Enabled = enabled;
    }

    /// <summary>The name for the UI.</summary>
    public string DisplayName { get; set; }
    /// <summary>A detailed description.</summary>
    public string Description { get; set; }
    /// <summary>Determines whether this authentication method is enabled.</summary>
    public bool Enabled { get; set; }
    /// <summary>An enumeration type for the <see cref="AuthenticationMethod"/>.</summary>
    public AuthenticationMethodType Type { get; protected set; }
    /// <summary>Authentication method security level.</summary>
    public AuthenticationMethodSecurityLevel SecurityLevel { get; protected set; }
}

/// <summary>SMS authentication method.</summary>
public class SmsAuthenticationMethod : AuthenticationMethod
{
    /// <summary>Creates a new instance of <see cref="SmsAuthenticationMethod"/> class.</summary>
    /// <param name="displayName">The name for the UI.</param>
    /// <param name="description">A detailed description.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    public SmsAuthenticationMethod(string displayName, string description, bool enabled = true) : base(displayName, description, enabled) {
        Type = AuthenticationMethodType.SMS;
        SecurityLevel = AuthenticationMethodSecurityLevel.Medium;
    }
}

/// <summary>FIDO2 authentication method.</summary>
public class Fido2AuthenticationMethod : AuthenticationMethod
{
    /// <summary>Creates a new instance of <see cref="Fido2AuthenticationMethod"/> class.</summary>
    /// <param name="displayName">The name for the UI.</param>
    /// <param name="description">A detailed description.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    public Fido2AuthenticationMethod(string displayName, string description, bool enabled = true) : base(displayName, description, enabled) {
        Type = AuthenticationMethodType.Fido2;
        SecurityLevel = AuthenticationMethodSecurityLevel.High;
    }
}

/// <summary>Microsoft Authenticator authentication method.</summary>
public class MicrosoftAuthenticatorAuthenticationMethod : AuthenticationMethod
{
    /// <summary>Creates a new instance of <see cref="MicrosoftAuthenticatorAuthenticationMethod"/> class.</summary>
    /// <param name="displayName">The name for the UI.</param>
    /// <param name="description">A detailed description.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    public MicrosoftAuthenticatorAuthenticationMethod(string displayName, string description, bool enabled = true) : base(displayName, description, enabled) {
        Type = AuthenticationMethodType.MicrosoftAuthenticator;
        SecurityLevel = AuthenticationMethodSecurityLevel.High;
    }
}

/// <summary>Biometrics authentication method.</summary>
public class BiometricsAuthenticationMethod : AuthenticationMethod
{
    /// <summary>Creates a new instance of <see cref="BiometricsAuthenticationMethod"/> class.</summary>
    /// <param name="displayName">The name for the UI.</param>
    /// <param name="description">A detailed description.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    public BiometricsAuthenticationMethod(string displayName, string description, bool enabled = true) : base(displayName, description, enabled) {
        Type = AuthenticationMethodType.Biometrics;
        SecurityLevel = AuthenticationMethodSecurityLevel.High;
    }
}

/// <summary>Email authentication method.</summary>
public class EmailAuthenticationMethod : AuthenticationMethod
{
    /// <summary>Creates a new instance of <see cref="EmailAuthenticationMethod"/> class.</summary>
    /// <param name="displayName">The name for the UI.</param>
    /// <param name="description">A detailed description.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    public EmailAuthenticationMethod(string displayName, string description, bool enabled = true) : base(displayName, description, enabled) {
        Type = AuthenticationMethodType.Email;
        SecurityLevel = AuthenticationMethodSecurityLevel.Medium;
    }
}