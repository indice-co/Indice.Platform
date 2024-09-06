﻿using System.Security;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.Core.Mvc.Localization;
using Indice.Features.Identity.Core.Mvc.Razor;
using Indice.Features.Identity.Core.Totp;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions to configure the <see cref="IServiceCollection"/> of an ASP.NET Core application.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Configures the cookie used by <see cref="ExtendedIdentityConstants.ExtendedValidationUserIdScheme"/>.</summary>
    /// <param name="services">The services available in the application.</param>
    /// <param name="configure">An action to configure the <see cref="CookieAuthenticationOptions"/>.</param>
    public static IServiceCollection ConfigureExtendedValidationCookie(this IServiceCollection services, Action<CookieAuthenticationOptions> configure)
        => services.Configure(ExtendedIdentityConstants.ExtendedValidationUserIdScheme, configure);

    /// <summary>Configures the cookie used by <see cref="ExtendedIdentityConstants.MfaOnboardingScheme"/>.</summary>
    /// <param name="services">The services available in the application.</param>
    /// <param name="configure">An action to configure the <see cref="CookieAuthenticationOptions"/>.</param>
    public static IServiceCollection ConfigureMfaOnboardingCookie(this IServiceCollection services, Action<CookieAuthenticationOptions> configure)
        => services.Configure(ExtendedIdentityConstants.MfaOnboardingScheme, configure);

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
}
