using System;
using System.Security;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Mvc.Localization;
using Indice.AspNetCore.Mvc.Razor;
using Indice.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Extensions to configure the <see cref="IServiceCollection"/> of an ASP.NET Core application.</summary>
    public static class IdentityServiceCollectionExtensions
    {
        /// <summary>Configures the cookie used by <see cref="ExtendedIdentityConstants.ExtendedValidationUserIdScheme"/>.</summary>
        /// <param name="services">The services available in the application.</param>
        /// <param name="configure">An action to configure the <see cref="CookieAuthenticationOptions"/>.</param>
        public static IServiceCollection ConfigureExtendedValidationCookie(this IServiceCollection services, Action<CookieAuthenticationOptions> configure)
            => services.Configure(ExtendedIdentityConstants.ExtendedValidationUserIdScheme, configure);

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
            services.AddScoped<IClientThemingService<TThemeConfig>, ClientThemingService<TThemeConfig>>();
            services.AddSingleton(serviceProvider => new ClientThemeConfigTypeResolver(typeof(TThemeConfig)));
            return services;
        }

        /// <summary>Adds the required services in order to access client theme data in the Views.</summary>
        /// <param name="services">The services available in the application.</param>
        public static IServiceCollection AddClientThemingService(this IServiceCollection services) => services.AddClientThemingService<DefaultClientThemeConfig>();

        /// <summary></summary>
        /// <param name="services"></param>
        /// <param name="configure"></param>
        public static IServiceCollection AddTotpServiceFactory(this IServiceCollection services, Action<TotpOptions> configure = null) {
            var serviceProvider = services.BuildServiceProvider();
            var totpSection = serviceProvider.GetRequiredService<IConfiguration>().GetSection(TotpOptions.Name);
            var totpOptions = new TotpOptions {
                CodeDuration = totpSection.GetValue<int?>(nameof(TotpOptions.CodeDuration)) ?? TotpOptions.DefaultCodeDuration,
                CodeLength = totpSection.GetValue<int?>(nameof(TotpOptions.CodeLength)) ?? TotpOptions.DefaultCodeLength,
                EnableDeveloperTotp = totpSection.GetValue<bool>(nameof(TotpOptions.EnableDeveloperTotp))
            };
            configure?.Invoke(totpOptions);
            services.TryAddSingleton(totpOptions);
            services.TryAddTransient<TotpServiceFactory>();
            services.TryAddSingleton(new Rfc6238AuthenticationService(totpOptions.Timestep, totpOptions.CodeLength));
            return services;
        }
    }
}
