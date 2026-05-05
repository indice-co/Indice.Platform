namespace Indice.Features.Identity.Core.Models;

/// <summary>Fluent builder for configuring authentication methods.</summary>
public class AuthenticationMethodFactoryBuilder
{
    private readonly List<AuthenticationMethodConfiguration> _configurations = new();

    internal IReadOnlyList<AuthenticationMethodConfiguration> Configurations => _configurations;
    
    internal Type AuthenticationProviderType { get; private set; } = typeof(AuthenticationMethodProviderInMemory);

    /// <summary>
    /// Specifies the authentication provider type to use for authentication method resolution.
    /// </summary>
    /// <remarks>Call this method to configure which authentication provider implementation will be used by
    /// the factory. Only one provider type can be set per builder instance.</remarks>
    /// <typeparam name="T">The type of the authentication method provider to register. Must implement IAuthenticationMethodProvider.</typeparam>
    /// <returns>The current instance of AuthenticationMethodFactoryBuilder for method chaining.</returns>
    public AuthenticationMethodFactoryBuilder UseAuthenticationProvider<T>() where T : IAuthenticationMethodProvider
    {
        AuthenticationProviderType = typeof(T);
        return this;
    }



    /// <summary>Adds a custom authentication method with a factory delegate for instance creation.</summary>
    /// <typeparam name="TAuthenticationMethod">The authentication method type.</typeparam>
    /// <param name="factory">Factory delegate that creates the authentication method instance.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    /// <param name="displayNameKey">Optional custom display name (overrides default localization).</param>
    /// <param name="descriptionKey">Optional custom description (overrides default localization).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Add&lt;MyAuthMethod&gt;(
    ///     factory: (name, desc, mfa, enabled) => new MyAuthMethod(name, desc, mfa, enabled),
    ///     displayNameKey: "My Method",
    ///     descriptionKey: "Custom authentication method");
    /// </code>
    /// </example>
    public AuthenticationMethodFactoryBuilder Add<TAuthenticationMethod>(
        AuthenticationMethodFactoryDelegate factory,
        bool supportsMfa = true,
        bool enabled = true,
        string? displayNameKey = null,
        string? descriptionKey = null) where TAuthenticationMethod : AuthenticationMethod {
        ArgumentNullException.ThrowIfNull(factory);

        _configurations.Add(new AuthenticationMethodConfiguration {
            MethodType = typeof(TAuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled,
            DisplayNameKey = displayNameKey,
            DescriptionKey = descriptionKey,
            Factory = (displayName, description, mfa, isEnabled) => factory(displayName, description, mfa, isEnabled)
        });
        return this;
    }

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
}
