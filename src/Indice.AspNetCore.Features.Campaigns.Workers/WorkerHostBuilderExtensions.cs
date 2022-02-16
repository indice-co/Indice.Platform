using Indice.AspNetCore.Features.Campaigns.Workers;
using Indice.Events;
using Indice.Hosting.Tasks;
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
            workerHostBuilder.Services.AddPlatformEventHandler<CampaignCreatedEvent, CampaignCreatedEventHandler>();
            workerHostBuilder.Services.TryAddTransient<Func<string, IPushNotificationService>>(serviceProvider => key => new PushNotificationServiceNoop());
            return workerHostBuilder.AddJob<CampaignCreatedJobHandler>()
                                    .WithQueueTrigger<CampaignQueueItem>(options => {
                                        options.QueueName = QueueNames.CampaignCreated;
                                        options.PollingInterval = TimeSpan.FromSeconds(30).TotalMilliseconds;
                                        options.InstanceCount = 1;
                                    })
                                    .AddJob<SendPushNotificationJobHandler>()
                                    .WithQueueTrigger<PushNotificationQueueItem>(options => {
                                        options.QueueName = QueueNames.SendPushNotification;
                                        options.PollingInterval = TimeSpan.FromSeconds(5).TotalMilliseconds;
                                        options.InstanceCount = 1;
                                    });
        }

        /// <summary>
        /// Adds an Azure specific implementation of <see cref="IPushNotificationService"/> for sending push notifications.
        /// </summary>
        /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
        /// <param name="configure">Configure the available options for push notifications. Null to use defaults.</param>
        public static void UsePushNotificationServiceAzure(this CampaignsJobsOptions options, Action<IServiceProvider, PushNotificationAzureOptions> configure = null) => 
            options.Services.AddPushNotificationServiceAzure(KeyedServiceNames.PushNotificationServiceAzureKey, configure);
    }
}
