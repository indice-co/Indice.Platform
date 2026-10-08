using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Indice.Features.Identity.Core.Models;

/// <summary>Fluent builder for configuring authentication methods.</summary>
public class AuthenticationMethodBuilder
{
    /// <summary>
    /// Creates a new instance of <see cref="AuthenticationMethodBuilder"/>.
    /// </summary>
    /// <param name="services">The service collection to which the authentication method provider will be registered.</param>
    public AuthenticationMethodBuilder(IServiceCollection services) {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }
    
    internal IServiceCollection Services { get; }
    
    /// <summary>
    /// Specifies the authentication provider type to use for authentication method resolution.
    /// </summary>
    /// <remarks>Call this method to configure which authentication provider implementation will be used by
    /// the factory. Only one provider type can be set per builder instance.</remarks>
    /// <param name="configureAction">An optional action to configure the list of authentication method configurations.</param>
    /// <returns>The current instance of AuthenticationMethodFactoryBuilder for method chaining.</returns>
    public AuthenticationMethodBuilder UseInMemoryProvider(Action<List<AuthenticationMethodConfiguration>>? configureAction) {
        var configuredMethods = new List<AuthenticationMethodConfiguration>();
        configureAction?.Invoke(configuredMethods);

        if (configuredMethods.Count == 0) {
            // Register the configured methods in the service collection
            configuredMethods.AddAuthenticatorApp();
            configuredMethods.AddSms();
        }

        foreach (var config in configuredMethods) {
            Services.AddSingleton(config);
            Services.AddScoped(typeof(AuthenticationMethod), config.MethodType);
        }
        Services.TryAddScoped<IAuthenticationMethodProvider, AuthenticationMethodProviderInMemory>();
        return this;
    }

}

/// <summary>
/// Provides extension methods for configuring authentication methods in a list of authentication method configurations.
/// </summary>
/// <remarks>These extension methods enable fluent addition of built-in and custom authentication methods to a
/// collection of authentication method configurations. Each method allows specifying whether the authentication method
/// supports multi-factor authentication (MFA) and whether it is enabled. Methods are intended for use during
/// authentication pipeline setup or configuration.</remarks>
public static class AuthenticationMethodBuilderExtensions
{
    /// <summary>Adds a custom authentication method.</summary>
    /// <typeparam name="TAuthenticationMethod">The type of the custom authentication method to add.</typeparam>
    /// <param name="configuredMethods">The list of configured authentication methods to which the custom method will be added.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    /// <returns>The list instance for method chaining.</returns>
    public static List<AuthenticationMethodConfiguration> Add<TAuthenticationMethod>(this List<AuthenticationMethodConfiguration> configuredMethods, 
        bool supportsMfa = true,
        bool enabled = true) where TAuthenticationMethod : AuthenticationMethod {
        configuredMethods.Add(new AuthenticationMethodConfiguration {
            MethodType = typeof(TAuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled,
        });
        return configuredMethods;
    }

    /// <summary>Adds SMS authentication method.</summary>
    /// <param name="configuredMethods">The list of configured authentication methods to which the custom method will be added.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param> 
    /// <returns>The list instance for method chaining.</returns>
    public static List<AuthenticationMethodConfiguration> AddSms(this List<AuthenticationMethodConfiguration> configuredMethods, bool supportsMfa = true, bool enabled = true) {
        configuredMethods.Add(new AuthenticationMethodConfiguration {
            MethodType = typeof(SmsAuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled
        });
        return configuredMethods;
    }

    /// <summary>Adds Email authentication method.</summary>
    /// <param name="configuredMethods">The list of configured authentication methods to which the custom method will be added.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param>
    /// <returns>The list instance for method chaining.</returns> 
    public static List<AuthenticationMethodConfiguration> AddEmail(this List<AuthenticationMethodConfiguration> configuredMethods, bool supportsMfa = true, bool enabled = true) {
        configuredMethods.Add(new AuthenticationMethodConfiguration {
            MethodType = typeof(EmailAuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled
        });
        return configuredMethods;
    }

    /// <summary>Adds Authenticator App authentication method.</summary>
    /// <param name="configuredMethods">The list of configured authentication methods to which the custom method will be added.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param> 
    /// <returns>The list instance for method chaining.</returns>
    public static List<AuthenticationMethodConfiguration> AddAuthenticatorApp(this List<AuthenticationMethodConfiguration> configuredMethods, bool supportsMfa = true, bool enabled = true) {
        configuredMethods.Add(new AuthenticationMethodConfiguration {
            MethodType = typeof(AuthenticatorAppAuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled
        }); 
        configuredMethods.Add(new AuthenticationMethodConfiguration {
            MethodType = typeof(RecoveryCodeAuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled
        });
        return configuredMethods;
    }

    /// <summary>Adds FIDO2 authentication method.</summary>
    /// <param name="configuredMethods">The list of configured authentication methods to which the custom method will be added.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param> 
    /// <returns>The list instance for method chaining.</returns>
    public static List<AuthenticationMethodConfiguration> AddFido2(this List<AuthenticationMethodConfiguration> configuredMethods, bool supportsMfa = true, bool enabled = true) {
        configuredMethods.Add(new AuthenticationMethodConfiguration {
            MethodType = typeof(Fido2AuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled
        });
        return configuredMethods;
    }

    /// <summary>Adds Viber authentication method.</summary>
    /// <param name="configuredMethods">The list of configured authentication methods to which the custom method will be added.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param> 
    /// <returns>The list instance for method chaining.</returns>
    public static List<AuthenticationMethodConfiguration> AddViber(this List<AuthenticationMethodConfiguration> configuredMethods, bool supportsMfa = true, bool enabled = true) {
        configuredMethods.Add(new AuthenticationMethodConfiguration {
            MethodType = typeof(ViberAuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled
        });
        return configuredMethods;
    }

    /// <summary>Adds Trusted Device authentication method.</summary>
    /// <param name="configuredMethods">The list of configured authentication methods to which the custom method will be added.</param>
    /// <param name="supportsMfa">Determines whether this authentication method participates in the MFA step.</param>
    /// <param name="enabled">Determines whether this authentication method is enabled.</param> 
    /// <returns>The list instance for method chaining.</returns>
    public static List<AuthenticationMethodConfiguration> AddTrustedDevice(this List<AuthenticationMethodConfiguration> configuredMethods, bool supportsMfa = true, bool enabled = true) {
        configuredMethods.Add(new AuthenticationMethodConfiguration {
            MethodType = typeof(TrustedDeviceAuthenticationMethod),
            SupportsMfa = supportsMfa,
            Enabled = enabled
        });
        return configuredMethods;
    }
}
