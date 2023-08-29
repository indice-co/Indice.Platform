using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core.Models;

/// <summary>Describes the various authentication methods for multi-factor authentication.</summary>
public abstract class AuthenticationMethod
{
    /// <summary>Constructor blueprint for <see cref="AuthenticationMethod"/>.</summary>
    /// <param name="displayName">The name for the UI.</param>
    /// <param name="description">A detailed description.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    public AuthenticationMethod(string displayName, string description, bool supportsMfa = true, bool enabled = true) {
        DisplayName = displayName;
        Description = description;
        Enabled = enabled;
        SupportsMfa = supportsMfa;
    }

    /// <summary>The name for the UI.</summary>
    public string DisplayName { get; }
    /// <summary>A detailed description.</summary>
    public string Description { get; }
    /// <summary>Determines whether this authentication method is enabled.</summary>
    public bool Enabled { get; }
    /// <summary>Determines whether this authentication method participates in the MFA step.</summary>
    public bool SupportsMfa { get; }
    /// <summary>An enumeration type for the <see cref="AuthenticationMethod"/>.</summary>
    public AuthenticationMethodType Type { get; protected set; }
    /// <summary>Authentication method security level.</summary>
    public AuthenticationMethodSecurityLevel SecurityLevel { get; protected set; }

    /// <summary>Determines whether the authentication method supports the use of a delivery channel.</summary>
    public bool SupportsDeliveryChannel() => typeof(IAuthenticationMethodWithChannel).IsAssignableFrom(GetType());

    /// <summary>Determines whether the authentication method supports the use of devices.</summary>
    public bool SupportsDevices() => typeof(IAuthenticationMethodWithDevices).IsAssignableFrom(GetType());

    /// <summary>Determines whether the authentication method has a token provider configured.</summary>
    public bool SupportsTokenProvider() => typeof(IAuthenticationMethodWithTokenProvider).IsAssignableFrom(GetType());

    /// <summary>Gets the <see cref="TotpDeliveryChannel"/> if the authentication method supports it.</summary>
    public TotpDeliveryChannel GetDeliveryChannel() => SupportsDeliveryChannel() ? ((IAuthenticationMethodWithChannel)this).DeliveryChannel : default;

    /// <summary>Gets the list of associated user devices if the authentication method supports it.</summary>
    public IEnumerable<UserDevice> GetDevices() => SupportsDevices() ? ((IAuthenticationMethodWithDevices)this).Devices : Enumerable.Empty<UserDevice>();

    /// <summary>Gets the token provider associated with this authentication method, if applicable.</summary>
    public string GetTokenProvider() => SupportsTokenProvider() ? ((IAuthenticationMethodWithTokenProvider)this).TokenProvider : default;
}

/// <summary>Authentication method that contains a delivery channel.</summary>
public interface IAuthenticationMethodWithChannel
{
    /// <summary>The delivery channel that can be used by the authentication method.</summary>
    public TotpDeliveryChannel DeliveryChannel { get; }
}

/// <summary>Authentication method that supports a delivery channel.</summary>
public interface IAuthenticationMethodWithDevices
{
    /// <summary>The devices that are supported by the authentication method.</summary>
    public IEnumerable<UserDevice> Devices { get; }
}

/// <summary>Authentication method that has a token provider configured.</summary>
public interface IAuthenticationMethodWithTokenProvider
{
    /// <summary>The name of the token provider.</summary>
    public string TokenProvider { get; }
}

/// <summary>SMS authentication method.</summary>
public class SmsAuthenticationMethod : AuthenticationMethod, IAuthenticationMethodWithChannel, IAuthenticationMethodWithTokenProvider
{
    /// <summary>Creates a new instance of <see cref="SmsAuthenticationMethod"/> class.</summary>
    /// <param name="displayName">The name for the UI.</param>
    /// <param name="description">A detailed description.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    public SmsAuthenticationMethod(string displayName, string description, bool supportsMfa = true, bool enabled = true) : base(displayName, description, supportsMfa, enabled) {
        Type = AuthenticationMethodType.PhoneNumber;
        SecurityLevel = AuthenticationMethodSecurityLevel.Medium;
    }

    /// <inheritdoc />
    public TotpDeliveryChannel DeliveryChannel { get; } = TotpDeliveryChannel.Sms;
    /// <inheritdoc />
    public string TokenProvider { get; } = TokenOptions.DefaultPhoneProvider;
}

/// <summary>FIDO2 authentication method.</summary>
public class Fido2AuthenticationMethod : AuthenticationMethod
{
    /// <summary>Creates a new instance of <see cref="Fido2AuthenticationMethod"/> class.</summary>
    /// <param name="displayName">The name for the UI.</param>
    /// <param name="description">A detailed description.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    public Fido2AuthenticationMethod(string displayName, string description, bool supportsMfa = true, bool enabled = true) : base(displayName, description, supportsMfa, enabled) {
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
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    public MicrosoftAuthenticatorAuthenticationMethod(string displayName, string description, bool supportsMfa = true, bool enabled = true) : base(displayName, description, supportsMfa, enabled) {
        Type = AuthenticationMethodType.MicrosoftAuthenticator;
        SecurityLevel = AuthenticationMethodSecurityLevel.High;
    }
}

/// <summary>Biometrics authentication method.</summary>
public class BiometricsAuthenticationMethod : AuthenticationMethod, IAuthenticationMethodWithChannel, IAuthenticationMethodWithDevices, IAuthenticationMethodWithTokenProvider
{
    /// <summary>Creates a new instance of <see cref="BiometricsAuthenticationMethod"/> class.</summary>
    /// <param name="displayName">The name for the UI.</param>
    /// <param name="description">A detailed description.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    public BiometricsAuthenticationMethod(string displayName, string description, bool supportsMfa = true, bool enabled = true) : base(displayName, description, supportsMfa, enabled) {
        Type = AuthenticationMethodType.Biometrics;
        SecurityLevel = AuthenticationMethodSecurityLevel.High;
    }

    /// <inheritdoc />
    public TotpDeliveryChannel DeliveryChannel { get; } = TotpDeliveryChannel.PushNotification;
    /// <inheritdoc />
    public IEnumerable<UserDevice> Devices { get; } = new List<UserDevice>();
    /// <inheritdoc />
    public string TokenProvider { get; } = TokenOptions.DefaultPhoneProvider;
}

/// <summary>Email authentication method.</summary>
public class EmailAuthenticationMethod : AuthenticationMethod, IAuthenticationMethodWithTokenProvider, IAuthenticationMethodWithChannel
{
    /// <summary>Creates a new instance of <see cref="EmailAuthenticationMethod"/> class.</summary>
    /// <param name="displayName">The name for the UI.</param>
    /// <param name="description">A detailed description.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    public EmailAuthenticationMethod(string displayName, string description, bool supportsMfa = true, bool enabled = true) : base(displayName, description, supportsMfa, enabled) {
        Type = AuthenticationMethodType.Email;
        SecurityLevel = AuthenticationMethodSecurityLevel.Medium;
    }

    /// <inheritdoc />
    public string TokenProvider { get; } = TokenOptions.DefaultEmailProvider;
    /// <inheritdoc />
    public TotpDeliveryChannel DeliveryChannel => TotpDeliveryChannel.Email;
}
