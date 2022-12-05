using System;
using FluentValidation.AspNetCore;
using Indice.AspNetCore.Identity.Api;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Adds feature extensions to the <see cref="IMvcBuilder"/>.</summary>
    public static class DevicesFeatureExtensions
    {
        /// <summary>Adds all required services and controllers for <b>Devices</b> feature.</summary>
        /// <param name="builder">An interface for configuring MVC services.</param>
        /// <param name="configureAction">Configuration used for <b>Devices</b> feature.</param>
        public static IMvcBuilder AddDevices(this IMvcBuilder builder, Action<DeviceOptions> configureAction = null) {
            var services = builder.Services;
            var options = new DeviceOptions {
                Services = services
            };
            configureAction?.Invoke(options);
            options.Services = null;
            services.Configure<DeviceOptions>(x => {
                x.DefaultTotpDeliveryChannel = options.DefaultTotpDeliveryChannel;
            });
            services.AddPushNotificationServiceNoop();
            services.TryAddTransient<IPlatformEventService, PlatformEventService>();
            builder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new DevicesFeatureProvider()));
            builder.AddFluentValidation(options => options.RegisterValidatorsFromAssemblyContaining<DevicesFeatureProvider>());
            return builder;
        }
    }

    /// <summary>Extension methods on <see cref="DeviceOptions"/> type.</summary>
    public static class DeviceOptionsExtensions
    {
        /// <summary>Adds an Azure specific implementation of <see cref="IPushNotificationService"/> for sending push notifications.</summary>
        /// <param name="deviceOptions">Options used to configure <b>Devices</b> feature.</param>
        /// <param name="configure">Configure the available options for push notifications. Null to use defaults.</param>
        public static void UsePushNotificationsServiceAzure(this DeviceOptions deviceOptions, Action<IServiceProvider, PushNotificationAzureOptions> configure = null) =>
            deviceOptions.Services.AddPushNotificationServiceAzure(configure);
    }
}
