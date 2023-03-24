namespace Indice.Features.Identity.Core.Models;

/// <summary>Describes the various authentication methods for multi-factor authentication.</summary>
public abstract class AuthenticationMethod
{
    /// <summary>Constructor blueprint for <see cref="AuthenticationMethod"/>.</summary>
    /// <param name="displayName">The name for the UI.</param>
    /// <param name="description">A detailed description.</param>
    public AuthenticationMethod(string displayName, string description) {
        DisplayName = displayName;
        Description = description;
    }

    /// <summary>The name for the UI.</summary>
    public string DisplayName { get; set; }
    /// <summary>A detailed description.</summary>
    public string Description { get; set; }
    /// <summary>An enumeration type for the <see cref="AuthenticationMethod"/>.</summary>
    public AuthenticationMethodType Type { get; protected set; }
    /// <summary>Authentication method security level.</summary>
    public AuthenticationMethodSecurityLevel SecurityLevel { get; protected set; }
}

/// <summary></summary>
public class SmsAuthenticationMethod : AuthenticationMethod
{
    /// <summary>Creates a new instance of <see cref="SmsAuthenticationMethod"/> class.</summary>
    /// <param name="displayName">The name for the UI.</param>
    /// <param name="description">A detailed description.</param>
    public SmsAuthenticationMethod(string displayName, string description) : base(displayName, description) {
        Type = AuthenticationMethodType.SMS;
        SecurityLevel = AuthenticationMethodSecurityLevel.Medium;
    }
}