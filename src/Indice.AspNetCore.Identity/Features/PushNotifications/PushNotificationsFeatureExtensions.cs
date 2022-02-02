using System;
using FluentValidation.AspNetCore;
using Indice.AspNetCore.Identity.Api;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Adds feature extensions to the <see cref="IMvcBuilder"/>.
    /// </summary>
    public static class PushNotificationsFeatureExtensions
    {
        /// <summary>
        /// Adds all required services in order for Push notifications to work.
        /// </summary>
        /// <param name="builder">An interface for configuring MVC services.</param>
        /// <param name="configure">Configuration used in the implementation of <see cref="IPushNotificationService"/> service.</param>
        public static IMvcBuilder AddPushNotifications(this IMvcBuilder builder, Action<PushNotificationAzureOptions> configure = null) {
            if (configure is null) {
                builder.Services.AddPushNotificationServiceNoOp();
                return builder;
            }
            var options = new PushNotificationAzureOptions();
            configure?.Invoke(options);
            if (string.IsNullOrWhiteSpace(options.ConnectionString) || string.IsNullOrWhiteSpace(options.NotificationHubPath)) {
                builder.Services.AddPushNotificationServiceNoOp();
            } else {
                builder.Services.AddPushNotificationServiceAzure(configure);
            }
            builder.Services.TryAddTransient<IPlatformEventService, PlatformEventService>();
            builder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new PushNotificationsFeatureProvider()));
            builder.AddFluentValidation(options => options.RegisterValidatorsFromAssemblyContaining<PushNotificationsFeatureProvider>());
            return builder;
        }
    }
}
