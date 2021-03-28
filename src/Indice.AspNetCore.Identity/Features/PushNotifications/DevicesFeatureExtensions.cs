using System;
using System.Security;
using IdentityServer4.Configuration;
using Indice.AspNetCore.Identity.Features;
using Indice.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Adds feature extensions to the <see cref="IMvcBuilder"/>.
    /// </summary>
    public static class DevicesFeatureExtensions
    {
        /// <summary>
        /// Adds all required stuff in order for Push notifications to work.
        /// </summary>
        /// <param name="builder">IdentityServer builder Interface.</param>
        /// <param name="configure">Configuration used in <see cref="Rfc6238AuthenticationService"/> service.</param>
        public static IMvcBuilder AddPushNotifications(this IMvcBuilder builder, Action<PushNotificationOptions> configure = null) => AddPushNotifications<PushNotificationServiceAzure>(builder, configure);

        /// <summary>
        /// Adds all required stuff in order for Push notifications to work.
        /// </summary>
        /// <typeparam name="TPushNotificationServiceAzure">The type of <see cref="IPushNotificationService"/> service implementation to use.</typeparam>
        /// <param name="builder">IdentityServer builder Interface.</param>
        /// <param name="configure">Configuration used in <see cref="Rfc6238AuthenticationService"/> service.</param>
        public static IMvcBuilder AddPushNotifications<TPushNotificationServiceAzure>(this IMvcBuilder builder, Action<PushNotificationOptions> configure = null) where TPushNotificationServiceAzure : class, IPushNotificationService {
            builder.Services.AddPushNotificationServiceAzure(configure);
            builder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new DevicesFeatureProvider()));
            return builder;
        }

        /// <summary>
        /// Adds all required stuff in order for Push notifications to work.
        /// </summary>
        /// <param name="builder">IdentityServer builder Interface.</param>
        /// <param name="configure">Configuration used in <see cref="Rfc6238AuthenticationService"/>.</param>
        public static IIdentityServerBuilder AddPushNotifications(this IIdentityServerBuilder builder, Action<PushNotificationOptions> configure = null) => AddPushNotifications<PushNotificationServiceAzure>(builder, configure);

        /// <summary>
        /// Adds all required stuff in order for Push notifications to work.
        /// </summary>
        /// <typeparam name="TPushNotificationServiceAzure">The type of <see cref="IPushNotificationService"/> service implementation to use.</typeparam>
        /// <param name="builder">IdentityServer builder Interface.</param>
        /// <param name="configure">Configuration used in <see cref="Rfc6238AuthenticationService"/> service.</param>
        public static IIdentityServerBuilder AddPushNotifications<TPushNotificationServiceAzure>(this IIdentityServerBuilder builder, Action<PushNotificationOptions> configure = null) where TPushNotificationServiceAzure : class, IPushNotificationService {
            builder.Services.AddPushNotificationServiceAzure(configure);
            builder.Services.Configure<IdentityServerOptions>((options) => {
                options.Discovery.CustomEntries.Add("devices", new {
                    endpoint = options.IssuerUri.TrimEnd('/') + "/devices"
                });
            });
            return builder;
        }
    }
}
