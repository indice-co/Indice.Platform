using Indice.AspNetCore.Features.Campaigns.Workers.Azure;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extension methods on <see cref="IHostBuilder"/> used to configure Azure Functions for campaign management system.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configures services used by the queue triggers used for campaign management system.
        /// </summary>
        /// <param name="hostBuilder">A program initialization abstraction.</param>
        /// <param name="configure">Configure action for <see cref="CampaignsOptions"/>.</param>
        public static IHostBuilder ConfigureCampaigns(this IHostBuilder hostBuilder, Action<CampaignsOptions> configure = null) {
            hostBuilder.ConfigureServices((context, services) => {
                var options = new CampaignsOptions {
                    Services = services
                };
                configure?.Invoke(options);
                services.AddPushNotificationServiceNoop();
                services.AddEventDispatcherNoop();
            });
            return hostBuilder;
        }

        /// <summary>
        /// Adds an Azure specific implementation of <see cref="IPushNotificationService"/> for sending push notifications.
        /// </summary>
        /// <param name="options">Options used when configuring campaign Azure Functions.</param>
        /// <param name="configure">Configure the available options for push notifications. Null to use defaults.</param>
        public static void UsePushNotificationServiceAzure(this CampaignsOptions options, Action<IServiceProvider, PushNotificationAzureOptions> configure = null) =>
            options.Services.AddPushNotificationServiceAzure(configure);

        /// <summary>
        /// Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.
        /// </summary>
        /// <param name="options">Options used when configuring campaign Azure Functions.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static void UseEventDispatcherAzure(this CampaignsOptions options, Action<IServiceProvider, EventDispatcherAzureOptions> configure = null) =>
            options.Services.AddEventDispatcherAzure(configure);
    }
}
