using System.Security;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.Core.Mvc.Localization;
using Indice.Features.Identity.Core.Mvc.Razor;
using Indice.Features.Identity.Core.Totp;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions to configure the <see cref="IServiceCollection"/> of an ASP.NET Core application.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Configures the cookie used by <see cref="ExtendedIdentityConstants.ExtendedValidationScheme"/>.</summary>
    /// <param name="services">The services available in the application.</param>
    /// <param name="configure">An action to configure the <see cref="CookieAuthenticationOptions"/>.</param>
    public static IServiceCollection ConfigureExtendedValidationCookie(this IServiceCollection services, Action<CookieAuthenticationOptions> configure)
        => services.Configure(ExtendedIdentityConstants.ExtendedValidationScheme, configure);

    /// <summary>Configures <see cref="RazorViewEngineOptions"/> by adding the <see cref="ClientAwareViewLocationExpander"/> in the list of available <see cref="IViewLocationExpander"/>.</summary>
    /// <param name="services">The services available in the application.</param>
    public static IServiceCollection AddClientAwareViewLocationExpander(this IServiceCollection services) {
        services.Configure<RazorViewEngineOptions>(options => options.ViewLocationExpanders.Add(new ClientAwareViewLocationExpander()));
        services.AddSingleton<IHtmlLocalizerFactory, ClientAwareHtmlLocalizerFactory>();
        return services;
    }

    /// <summary>Adds the required services in order to access client theme data in the Views.</summary>
    /// <param name="services">The services available in the application.</param>
    public static IServiceCollection AddClientThemingService<TThemeConfig>(this IServiceCollection services) where TThemeConfig : class {
        services.TryAddScoped<IClientThemingService<TThemeConfig>, ClientThemingService<TThemeConfig>>();
        services.TryAddSingleton(serviceProvider => new ClientThemeConfigTypeResolver(typeof(TThemeConfig)));
        return services;
    }

    /// <summary>Adds the required services in order to access client theme data in the Views.</summary>
    /// <param name="services">The services available in the application.</param>
    public static IServiceCollection AddClientThemingService(this IServiceCollection services) => services.AddClientThemingService<DefaultClientThemeConfig>();

    /// <summary></summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="configure"></param>
    public static IServiceCollection AddTotpServiceFactory(this IServiceCollection services, IConfiguration configuration, Action<TotpOptions>? configure = null) {
        var totpSection = configuration.GetSection(TotpOptions.Name);
        var totpOptions = new TotpOptions {
            CodeDuration = totpSection.GetValue<int?>(nameof(TotpOptions.CodeDuration)) ?? TotpOptionsBase.DefaultCodeDuration,
            CodeLength = totpSection.GetValue<int?>(nameof(TotpOptions.CodeLength)) ?? TotpOptionsBase.DefaultCodeLength,
            EnableDeveloperTotp = totpSection.GetValue<bool>(nameof(TotpOptions.EnableDeveloperTotp))
        };
        configure?.Invoke(totpOptions);
        services.Configure<TotpOptions>(options => {
            options.CodeDuration = totpOptions.CodeDuration;
            options.CodeLength = totpOptions.CodeLength;
            options.EnableDeveloperTotp = totpOptions.EnableDeveloperTotp;
        });
        services.TryAddTransient<TotpServiceFactory>();
        services.TryAddSingleton(new Rfc6238AuthenticationService(totpOptions.Timestep, totpOptions.CodeLength));
        return services;
    }

    /// <summary>
    /// Configures the OpenIdConnect handlers and OAuth based handlers to persist the state parameter into the server-side IDistributedCache.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="schemes">The schemes to configure. If none provided, then all OpenIdConnect schemes will use the cache.</param>
    public static IServiceCollection AddExternalProviderStateDataFormatterCache(this IServiceCollection services, params string[] schemes) {
        services.AddSingleton<IPostConfigureOptions<OpenIdConnectOptions>>(svcs => new ConfigureExternalProviderOptions(schemes, svcs));
        services.AddSingleton<IPostConfigureOptions<OAuthOptions>>(svcs => new ConfigureExternalProviderOptions(schemes, svcs));
        return services;
    }

    /// <summary>
    /// Adds OAuth state data formatter cache configuration for external providers to the service collection.
    /// </summary>
    /// <typeparam name="TOptions">The OAuth options type to configure.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="schemes">The authentication schemes to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExternalProviderOAuthStateDataFormatterCache<TOptions>(this IServiceCollection services, params string[] schemes) where TOptions : OAuthOptions {
        services.AddSingleton<IPostConfigureOptions<TOptions>>(svcs => new ConfigureOAuthOptions<TOptions>(schemes, svcs));
        return services;
    }

    /// <summary>
    /// Adds external provider OpenID Connect state data formatter cache configuration for the specified authentication
    /// schemes.
    /// </summary>
    /// <typeparam name="TOptions">The type of <see cref="OpenIdConnectOptions"/> to configure.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="schemes">The authentication schemes to configure.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddExternalProviderOidStateDataFormatterCache<TOptions>(this IServiceCollection services, params string[] schemes) where TOptions : OpenIdConnectOptions {
        services.AddSingleton<IPostConfigureOptions<TOptions>>(svcs => new ConfigureOpenIdOptions<TOptions>(schemes, svcs));
        return services;
    }
}
