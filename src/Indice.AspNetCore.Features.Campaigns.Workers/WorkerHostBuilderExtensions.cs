using Indice.AspNetCore.Features.Campaigns;
using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.AspNetCore.Features.Campaigns.Workers;
using Indice.Hosting;
using Indice.Hosting.Services;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods on <see cref="WorkerHostBuilder"/> type.
    /// </summary>
    public static class WorkerHostBuilderExtensions
    {
        /// <summary>
        /// Adds the job handlers required the for campaigns feature.
        /// </summary>
        /// <param name="workerHostBuilder">A helper class to configure the worker host.</param>
        /// <param name="configure"></param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        public static WorkerHostBuilder AddCampaignsJobs(this WorkerHostBuilder workerHostBuilder, Action<CampaignsJobsOptions> configure = null) {
            var options = new CampaignsJobsOptions {
                Services = workerHostBuilder.Services
            };
            configure?.Invoke(options);
            options.Services = null;
            workerHostBuilder.AddJob<CampaignCreatedJobHandler>()
                             .WithQueueTrigger<CampaignCreatedEvent>(options => {
                                 options.QueueName = QueueNames.CampaignCreated;
                                 options.PollingInterval = TimeSpan.FromSeconds(30).TotalMilliseconds;
                                 options.InstanceCount = 1;
                             })
                             .AddJob<SendPushNotificationJobHandler>()
                             .WithQueueTrigger<SendPushNotificationEvent>(options => {
                                 options.QueueName = QueueNames.SendPushNotification;
                                 options.PollingInterval = TimeSpan.FromSeconds(5).TotalMilliseconds;
                                 options.InstanceCount = 1;
                             });
            workerHostBuilder.Services.TryAddTransient<Func<string, IPushNotificationService>>(serviceProvider => key => new PushNotificationServiceNoop());
            workerHostBuilder.Services.TryAddTransient<Func<string, IEventDispatcher>>(serviceProvider => key => new EventDispatcherNoop());
            return workerHostBuilder;
        }

        /// <summary>
        /// Adds an Azure specific implementation of <see cref="IPushNotificationService"/> for sending push notifications.
        /// </summary>
        /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
        /// <param name="configure">Configure the available options for push notifications. Null to use defaults.</param>
        public static void UsePushNotificationServiceAzure(this CampaignsJobsOptions options, Action<IServiceProvider, PushNotificationAzureOptions> configure = null) =>
            options.Services.AddPushNotificationServiceAzure(KeyedServiceNames.PushNotificationServiceAzureKey, configure);

        /// <summary>
        /// Adds <see cref="IEventDispatcher"/> using Indice worker host as a queuing mechanism.
        /// </summary>
        /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
        public static void UsePushNotificationServiceAzure(this CampaignsJobsOptions options) =>
            options.Services.AddKeyedService<IEventDispatcher, EventDispatcherHosting, string>(
                key: KeyedServiceNames.EventDispatcherAzureServiceKey,
                serviceProvider => new EventDispatcherHosting(new MessageQueueFactory(serviceProvider)),
                serviceLifetime: ServiceLifetime.Transient
            );
    }
}
