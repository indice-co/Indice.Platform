using System;
using System.Linq;
using System.Security;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Authorization;
using Indice.Configuration;
using Indice.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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
        public static IServiceCollection ConfigureExtendedValidationCookie(this IServiceCollection services, Action<CookieAuthenticationOptions> configure)
            => services.Configure(ExtendedIdentityConstants.ExtendedValidationUserIdScheme, configure);

        /// <summary>
        /// Registers an implementation of <see cref="IIdentityServerApiEventHandler{TEvent}"/> for the specified event type.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to handler.</typeparam>
        /// <typeparam name="TEventHandler">The handler to user for the specified event.</typeparam>
        /// <param name="services">The services available in the application.</param>
        public static IServiceCollection AddEventHandler<TEvent, TEventHandler>(this IServiceCollection services)
            where TEvent : IIdentityServerApiEvent
            where TEventHandler : class, IIdentityServerApiEventHandler<TEvent> {
            services.TryAddTransient(typeof(IIdentityServerApiEventHandler<TEvent>), typeof(TEventHandler));
            return services;
        }

        internal static IServiceCollection AddDefaultTotpService(this IServiceCollection services, Action<TotpOptions> configure = null) {
            var serviceProvider = services.BuildServiceProvider();
            var totpSection = serviceProvider.GetRequiredService<IConfiguration>().GetSection(TotpOptions.Name);
            var totpOptions = new TotpOptions {
                Services = services,
                CodeDuration = totpSection.GetValue<int?>(nameof(TotpOptions.CodeDuration)) ?? TotpOptions.DefaultCodeDuration,
                CodeLength = totpSection.GetValue<int?>(nameof(TotpOptions.CodeLength)) ?? TotpOptions.DefaultCodeLength,
                EnableDeveloperTotp = totpSection.GetValue<bool>(nameof(TotpOptions.EnableDeveloperTotp))
            };
            var hostingEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            configure?.Invoke(totpOptions);
            totpOptions.Services = null;
            services.TryAddSingleton(totpOptions);
            services.TryAddTransient<IPushNotificationService, DefaultPushNotificationService>();
            services.TryAddTransient<ITotpService, TotpService>();
            services.TryAddSingleton(new Rfc6238AuthenticationService(totpOptions.Timestep, totpOptions.CodeLength));
            if (totpOptions.EnableDeveloperTotp && !hostingEnvironment.IsProduction()) {
                var implementation = services.LastOrDefault(x => x.ServiceType == typeof(ITotpService))?.ImplementationType;
                if (implementation != null) {
                    var decoratorType = typeof(DeveloperTotpService);
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
        internal static IServiceCollection AddPushNotificationServiceAzure(this IServiceCollection services, Action<PushNotificationOptions> configure = null) {
            services.AddTransient<IPushNotificationService, PushNotificationServiceAzure>(serviceProvider => {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var options = new PushNotificationOptions {
                    ConnectionString = configuration.GetConnectionString(PushNotificationServiceAzure.ConnectionStringName),
                    NotificationHubPath = configuration.GetValue<string>(PushNotificationServiceAzure.NotificationsHubPath)
                };
                configure?.Invoke(options);
                return new PushNotificationServiceAzure(options);
            });
            return services;
        }
    }
}
