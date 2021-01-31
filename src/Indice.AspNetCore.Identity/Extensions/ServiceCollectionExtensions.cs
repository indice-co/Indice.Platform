using System;
using System.Linq;
using System.Security;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Authorization;
using Indice.Configuration;
using Indice.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions to configure the <see cref="IServiceCollection"/> of an ASP.NET Core application.
    /// </summary>
    public static class IdentityServiceCollectionExtensions
    {
        /// <summary>
        /// Configures the cookie used by <see cref="ExtendedIdentityConstants.ExtendedValidationUserIdScheme"/>.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <param name="configure">An action to configure the <see cref="CookieAuthenticationOptions"/>.</param>
        /// <returns>The services.</returns>
        public static IServiceCollection ConfigureExtendedValidationCookie(this IServiceCollection services, Action<CookieAuthenticationOptions> configure)
            => services.Configure(ExtendedIdentityConstants.ExtendedValidationUserIdScheme, configure);

        internal static IServiceCollection AddDefaultTotpService(this IServiceCollection services, Action<TotpOptions> configure = null) {
            var serviceProvider = services.BuildServiceProvider();
            var totpSection = serviceProvider.GetRequiredService<IConfiguration>().GetSection(TotpOptions.Name);
            var totpOptions = new TotpOptions {
                Services = services,
                CodeDuration = totpSection.GetValue<int?>(nameof(TotpOptions.CodeDuration)) ?? TotpOptions.DefaultCodeDuration,
                CodeLength = totpSection.GetValue<int?>(nameof(TotpOptions.CodeLength)) ?? TotpOptions.DefaultCodeLength
            };
            configure?.Invoke(totpOptions);
            totpOptions.Services = null;
            services.TryAddTransient<IPushNotificationService, DefaultPushNotificationService>();
            services.TryAddTransient<ITotpService, TotpService>();
            services.TryAddSingleton(new Rfc6238AuthenticationService(totpOptions.Timestep, totpOptions.CodeLength));
            if (totpOptions.EnablePersistedCodes) {
                var implementation = services.Where(x => x.ServiceType == typeof(ITotpService)).LastOrDefault()?.ImplementationType;
                if (implementation != null) {
                    var decoratorType = typeof(PersistedTotpService);
                    services.TryAddTransient(implementation);
                    services.AddTransient(typeof(ITotpService), decoratorType);
                }
            }
            return services;
        }

        /// <summary>
        /// Adds an instance of <see cref="IPushNotificationService"/> using Microsoft.Azure.NotificationHubs for sending push notifications
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        internal static IServiceCollection AddPushNotificationServiceAzure(this IServiceCollection services, Action<PushNotificationServiceAzure.PushNotificationOptions> configure = null) {
            services.AddTransient<IPushNotificationService, PushNotificationServiceAzure>(serviceProvider => {
                var options = new PushNotificationServiceAzure.PushNotificationOptions {
                    ConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(PushNotificationServiceAzure.CONNECTION_STRING_NAME),
                    NotificationHubPath = serviceProvider.GetRequiredService<IConfiguration>().GetValue<string>(PushNotificationServiceAzure.NOTIFICATIONS_HUB_PATH)
                };
                configure?.Invoke(options);
                return new PushNotificationServiceAzure(options);
            });
            return services;
        }
    }
}
