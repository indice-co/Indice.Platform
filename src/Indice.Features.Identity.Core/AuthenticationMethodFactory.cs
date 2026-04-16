using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.Core;

/// <summary>
/// Default implementation of <see cref="IAuthenticationMethodFactory"/> that creates localized authentication methods.
/// </summary>
public class AuthenticationMethodFactory : IAuthenticationMethodFactory
{
    private readonly IdentityMessageDescriber? _messageDescriber;
    private readonly IReadOnlyList<AuthenticationMethodConfiguration> _configurations;

    /// <summary>Creates a new instance of <see cref="AuthenticationMethodFactory"/>.</summary>
    /// <param name="configurations">The authentication method configurations.</param>
    /// <param name="messageDescriber">The message describer for localized strings (optional).</param>
    public AuthenticationMethodFactory(
        IEnumerable<AuthenticationMethodConfiguration> configurations,
        IdentityMessageDescriber? messageDescriber = null)
    {
        _configurations = configurations?.ToList() ?? throw new ArgumentNullException(nameof(configurations));
        _messageDescriber = messageDescriber;
    }

    /// <inheritdoc />
    public AuthenticationMethod[] GetAll()
    {
        return _configurations
            .Select(CreateMethod)
            .Where(m => m != null)
            .Cast<AuthenticationMethod>()
            .OrderByDescending(x => x.SecurityLevel)
            .ToArray();
    }

    /// <inheritdoc />
    public AuthenticationMethod? GetByCode(string code)
    {
        return GetAll().FirstOrDefault(m => m.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public T? Get<T>() where T : AuthenticationMethod
    {
        var config = _configurations.FirstOrDefault(c => c.MethodType == typeof(T));
        return config != null ? CreateMethod(config) as T : null;
    }

    /// <summary>Creates an authentication method instance from configuration.</summary>
    private AuthenticationMethod? CreateMethod(AuthenticationMethodConfiguration config)
    {
        // Get localized strings
        var (displayName, description) = GetLocalizedStrings(config);

        // Create the instance based on type
        return config.MethodType.Name switch
        {
            nameof(SmsAuthenticationMethod) => 
                new SmsAuthenticationMethod(displayName, description, config.SupportsMfa, config.Enabled),

            nameof(EmailAuthenticationMethod) => 
                new EmailAuthenticationMethod(displayName, description, config.SupportsMfa, config.Enabled),

            nameof(AuthenticatorAppAuthenticationMethod) => 
                new AuthenticatorAppAuthenticationMethod(displayName, description, config.SupportsMfa, config.Enabled),

            nameof(Fido2AuthenticationMethod) => 
                new Fido2AuthenticationMethod(displayName, description, config.SupportsMfa, config.Enabled),

            nameof(ViberAuthenticationMethod) => 
                new ViberAuthenticationMethod(displayName, description, config.SupportsMfa, config.Enabled),

            nameof(TrustedDeviceAuthenticationMethod) => 
                new TrustedDeviceAuthenticationMethod(displayName, description, config.SupportsMfa, config.Enabled),

            _ => null // Unknown type
        };
    }

    /// <summary>Gets localized display name and description for an authentication method.</summary>
    private (string displayName, string description) GetLocalizedStrings(AuthenticationMethodConfiguration config)
    {
        // If no message describer, use default English strings
        if (_messageDescriber == null)
        {
            return GetDefaultStrings(config);
        }

        // Use custom keys if provided, otherwise use type-based defaults
        var displayName = config.DisplayNameKey != null 
            ? config.DisplayNameKey // If custom key provided, use it as-is (should be localized by caller)
            : GetLocalizedDisplayName(config.MethodType);

        var description = config.DescriptionKey != null
            ? config.DescriptionKey // If custom key provided, use it as-is (should be localized by caller)
            : GetLocalizedDescription(config.MethodType);

        return (displayName, description);
    }

    /// <summary>Gets default (fallback) strings when localizer is not available.</summary>
    private static (string displayName, string description) GetDefaultStrings(AuthenticationMethodConfiguration config)
    {
        return config.MethodType.Name switch
        {
            nameof(SmsAuthenticationMethod) => 
                ("SMS", "Users will receive a text message containing a verification code."),

            nameof(EmailAuthenticationMethod) => 
                ("Email", "Users will receive a TOTP in their verified email address."),

            nameof(AuthenticatorAppAuthenticationMethod) => 
                ("Authenticator (recommended)", "Use an authenticator app to generate verification codes."),

            nameof(Fido2AuthenticationMethod) => 
                ("FIDO2", "Use a hardware security key for authentication."),

            nameof(ViberAuthenticationMethod) => 
                ("Viber", "Users will receive a Viber message containing a verification code."),

            nameof(TrustedDeviceAuthenticationMethod) => 
                ("Push notification", "Provide a push notification using a trusted device."),

            _ => ("Unknown", "Unknown authentication method")
        };
    }

    /// <summary>Gets localized display name from message describer based on method type.</summary>
    private string GetLocalizedDisplayName(Type methodType)
    {
        if (_messageDescriber == null) return methodType.Name;

        return methodType.Name switch
        {
            nameof(SmsAuthenticationMethod) => _messageDescriber.AuthMethod_Sms_DisplayName,
            nameof(EmailAuthenticationMethod) => _messageDescriber.AuthMethod_Email_DisplayName,
            nameof(AuthenticatorAppAuthenticationMethod) => _messageDescriber.AuthMethod_AuthenticatorApp_DisplayName,
            nameof(Fido2AuthenticationMethod) => _messageDescriber.AuthMethod_Fido2_DisplayName,
            nameof(ViberAuthenticationMethod) => _messageDescriber.AuthMethod_Viber_DisplayName,
            nameof(TrustedDeviceAuthenticationMethod) => _messageDescriber.AuthMethod_TrustedDevice_DisplayName,
            _ => methodType.Name
        };
    }

    /// <summary>Gets localized description from message describer based on method type.</summary>
    private string GetLocalizedDescription(Type methodType)
    {
        if (_messageDescriber == null) return string.Empty;

        return methodType.Name switch
        {
            nameof(SmsAuthenticationMethod) => _messageDescriber.AuthMethod_Sms_Description,
            nameof(EmailAuthenticationMethod) => _messageDescriber.AuthMethod_Email_Description,
            nameof(AuthenticatorAppAuthenticationMethod) => _messageDescriber.AuthMethod_AuthenticatorApp_Description,
            nameof(Fido2AuthenticationMethod) => _messageDescriber.AuthMethod_Fido2_Description,
            nameof(ViberAuthenticationMethod) => _messageDescriber.AuthMethod_Viber_Description,
            nameof(TrustedDeviceAuthenticationMethod) => _messageDescriber.AuthMethod_TrustedDevice_Description,
            _ => string.Empty
        };
    }
}
