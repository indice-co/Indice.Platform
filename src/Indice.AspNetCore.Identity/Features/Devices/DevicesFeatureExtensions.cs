using System;
using System.Security;
using FluentValidation.AspNetCore;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Api;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        /// <typeparam name="TPushNotificationService">The type of <see cref="IPushNotificationService"/> service implementation to use.</typeparam>
        /// <param name="builder">IdentityServer builder Interface.</param>
        /// <param name="configure">Configuration used in <see cref="Rfc6238AuthenticationService"/> service.</param>
        public static IMvcBuilder AddPushNotifications<TPushNotificationService>(this IMvcBuilder builder, Action<PushNotificationOptions> configure = null) where TPushNotificationService : class, IPushNotificationService {
            builder.Services.AddPushNotificationServiceAzure(configure);
            builder.Services.TryAddTransient<IPlatformEventService, EventService>();
            builder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new DevicesFeatureProvider()));
            builder.AddFluentValidation(options => options.RegisterValidatorsFromAssemblyContaining<DevicesFeatureProvider>());
            return builder;
        }
    }
}
