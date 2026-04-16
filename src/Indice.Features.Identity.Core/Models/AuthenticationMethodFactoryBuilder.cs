namespace Indice.Features.Identity.Core.Models;

/// <summary>Fluent builder for configuring authentication methods.</summary>
public class AuthenticationMethodFactoryBuilder
{
    private readonly List<AuthenticationMethodConfiguration> _configurations = new();

    internal IReadOnlyList<AuthenticationMethodConfiguration> Configurations => _configurations;

    /// <summary>Adds SMS authentication method.</summary>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public AuthenticationMethodFactoryBuilder AddSms(bool supportsMfa = true, bool enabled = true)
    {
        _configurations.Add(new AuthenticationMethodConfiguration
        {
            MethodType = typeof(SmsAuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled
        });
        return this;
    }

    /// <summary>Adds Email authentication method.</summary>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public AuthenticationMethodFactoryBuilder AddEmail(bool supportsMfa = true, bool enabled = true)
    {
        _configurations.Add(new AuthenticationMethodConfiguration
        {
            MethodType = typeof(EmailAuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled
        });
        return this;
    }

    /// <summary>Adds Authenticator App authentication method.</summary>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public AuthenticationMethodFactoryBuilder AddAuthenticatorApp(bool supportsMfa = true, bool enabled = true)
    {
        _configurations.Add(new AuthenticationMethodConfiguration
        {
            MethodType = typeof(AuthenticatorAppAuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled
        });
        return this;
    }

    /// <summary>Adds FIDO2 authentication method.</summary>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public AuthenticationMethodFactoryBuilder AddFido2(bool supportsMfa = true, bool enabled = true)
    {
        _configurations.Add(new AuthenticationMethodConfiguration
        {
            MethodType = typeof(Fido2AuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled
        });
        return this;
    }

    /// <summary>Adds Viber authentication method.</summary>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public AuthenticationMethodFactoryBuilder AddViber(bool supportsMfa = true, bool enabled = true)
    {
        _configurations.Add(new AuthenticationMethodConfiguration
        {
            MethodType = typeof(ViberAuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled
        });
        return this;
    }

    /// <summary>Adds Trusted Device authentication method.</summary>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public AuthenticationMethodFactoryBuilder AddTrustedDevice(bool supportsMfa = true, bool enabled = true)
    {
        _configurations.Add(new AuthenticationMethodConfiguration
        {
            MethodType = typeof(TrustedDeviceAuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled
        });
        return this;
    }

    /// <summary>Adds a custom authentication method with optional localization keys.</summary>
    /// <typeparam name="T">The authentication method type.</typeparam>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    /// <param name="displayNameKey">Optional custom display name (overrides default localization).</param>
    /// <param name="descriptionKey">Optional custom description (overrides default localization).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public AuthenticationMethodFactoryBuilder AddCustom<T>(
        bool supportsMfa = true, 
        bool enabled = true,
        string? displayNameKey = null,
        string? descriptionKey = null) where T : AuthenticationMethod
    {
        return AddCustom(typeof(T), supportsMfa, enabled, displayNameKey, descriptionKey);
    }

    /// <summary>Adds a custom authentication method with optional localization keys.</summary>
    /// <param name="methodType">The authentication method type.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    /// <param name="displayNameKey">Optional custom display name (overrides default localization).</param>
    /// <param name="descriptionKey">Optional custom description (overrides default localization).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public AuthenticationMethodFactoryBuilder AddCustom(
        Type methodType,
        bool supportsMfa = true, 
        bool enabled = true,
        string? displayNameKey = null,
        string? descriptionKey = null)
    {
        _configurations.Add(new AuthenticationMethodConfiguration
        {
            MethodType = methodType,
            SupportsMfa = supportsMfa,
            Enabled = enabled,
            DisplayNameKey = displayNameKey,
            DescriptionKey = descriptionKey
        });
        return this;
    }
}
