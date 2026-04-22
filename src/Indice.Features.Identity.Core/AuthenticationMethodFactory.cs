using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.Core;

/// <summary>
/// Default implementation of <see cref="IAuthenticationMethodFactory"/> that creates localized authentication methods.
/// </summary>
public class AuthenticationMethodFactory : IAuthenticationMethodFactory
{
    private readonly IdentityMessageDescriber _messageDescriber;
    private readonly IReadOnlyList<AuthenticationMethodConfiguration> _configurations;

    /// <summary>Creates a new instance of <see cref="AuthenticationMethodFactory"/>.</summary>
    /// <param name="configurations">The authentication method configurations.</param>
    /// <param name="messageDescriber">The message describer for localized strings (optional).</param>
    public AuthenticationMethodFactory(
        IEnumerable<AuthenticationMethodConfiguration> configurations,
        IdentityMessageDescriber messageDescriber) {
        _configurations = configurations?.ToList() ?? throw new ArgumentNullException(nameof(configurations));
        _messageDescriber = messageDescriber;
    }

    /// <inheritdoc />
    public AuthenticationMethod[] GetAll() {
        return _configurations
            .Select(CreateMethod)
            .Where(m => m != null)
            .Cast<AuthenticationMethod>()
            .OrderByDescending(x => x.SecurityLevel)
            .ToArray();
    }

    /// <inheritdoc />
    public AuthenticationMethod? GetByCode(string code) {
        return GetAll().FirstOrDefault(m => m.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public T? Get<T>() where T : AuthenticationMethod {
        var config = _configurations.FirstOrDefault(c => c.MethodType == typeof(T));
        return config != null ? CreateMethod(config) as T : null;
    }

    /// <summary>Creates an authentication method instance from configuration.</summary>
    private AuthenticationMethod? CreateMethod(AuthenticationMethodConfiguration config) {
        // Get localized strings
        var (displayName, description) = GetLocalizedStrings(config);

        // If a custom factory delegate is provided, use it
        if (config.Factory != null) {
            return config.Factory(displayName, description, config.SupportsMfa, config.Enabled);
        }

        // Create the instance based on built-in type
        return config.MethodType.Name switch {
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

            // Unknown type without factory - throw descriptive error
            _ => throw new InvalidOperationException(
                $"Cannot create instance of authentication method '{config.MethodType.FullName}'. " +
                $"For custom authentication methods, use AddCustom<T>(factory, ...) overload with a factory delegate. " +
                $"Example: AddCustom<MyMethod>((name, desc, mfa, enabled) => new MyMethod(name, desc, mfa, enabled))")
        };
    }

    /// <summary>Gets localized display name and description for an authentication method.</summary>
    private (string displayName, string description) GetLocalizedStrings(AuthenticationMethodConfiguration config) {
        // Use custom keys if provided, otherwise use type-based defaults
        var displayName = config.DisplayNameKey != null
            ? config.DisplayNameKey // If custom key provided, use it as-is (should be localized by caller)
            : GetLocalizedDisplayName(config.MethodType);

        var description = config.DescriptionKey != null
            ? config.DescriptionKey // If custom key provided, use it as-is (should be localized by caller)
            : GetLocalizedDescription(config.MethodType);

        return (displayName, description);
    }

    /// <summary>Gets localized display name from message describer based on method type.</summary>
    private string GetLocalizedDisplayName(Type methodType) {
        if (_messageDescriber == null) return methodType.Name;

        return methodType.Name switch {
            nameof(SmsAuthenticationMethod) => _messageDescriber.AuthMethod_Sms_DisplayName,
            nameof(EmailAuthenticationMethod) => _messageDescriber.AuthMethod_Email_DisplayName,
            nameof(AuthenticatorAppAuthenticationMethod) => _messageDescriber.AuthMethod_AuthenticatorApp_DisplayName,
            nameof(Fido2AuthenticationMethod) => _messageDescriber.AuthMethod_Fido2_DisplayName,
            nameof(ViberAuthenticationMethod) => _messageDescriber.AuthMethod_Viber_DisplayName,
            nameof(TrustedDeviceAuthenticationMethod) => _messageDescriber.AuthMethod_TrustedDevice_DisplayName,
            _ => _messageDescriber.GetGenericString($"AuthMethod_{methodType.Name}_DisplayName") ?? methodType.Name
        };
    }

    /// <summary>Gets localized description from message describer based on method type.</summary>
    private string GetLocalizedDescription(Type methodType) {
        if (_messageDescriber == null) return string.Empty;

        return methodType.Name switch {
            nameof(SmsAuthenticationMethod) => _messageDescriber.AuthMethod_Sms_Description,
            nameof(EmailAuthenticationMethod) => _messageDescriber.AuthMethod_Email_Description,
            nameof(AuthenticatorAppAuthenticationMethod) => _messageDescriber.AuthMethod_AuthenticatorApp_Description,
            nameof(Fido2AuthenticationMethod) => _messageDescriber.AuthMethod_Fido2_Description,
            nameof(ViberAuthenticationMethod) => _messageDescriber.AuthMethod_Viber_Description,
            nameof(TrustedDeviceAuthenticationMethod) => _messageDescriber.AuthMethod_TrustedDevice_Description,
            _ => _messageDescriber.GetGenericString($"AuthMethod_{methodType.Name}_Description") ?? methodType.Name
        };
    }
}