using System;
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
            services.TryAddTransient<IPushNotificationService, DefaultPushNotificationService>();
            services.TryAddTransient<ITotpService, TotpService>();
            services.TryAddSingleton(serviceProvider => {
                var options = new TotpOptions {
                    TokenDuration = serviceProvider.GetRequiredService<IConfiguration>().GetSection(TotpOptions.Name).GetValue<int?>(nameof(TotpOptions.TokenDuration))
                };
                configure?.Invoke(options);
                return new Rfc6238AuthenticationService(options);
            });
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
