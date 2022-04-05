using Indice.AspNetCore.Features.Campaigns.Workers;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Hosting;
using Indice.Hosting.Services;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            workerHostBuilder.AddJob<CampaignPublishedJobHandler>().WithQueueTrigger<CampaignPublishedEvent>(options => {
                options.QueueName = EventNames.CampaignPublished;
                options.PollingInterval = TimeSpan.FromSeconds(5).TotalMilliseconds;
                options.InstanceCount = 1;
            })
            .AddJob<ResolveMessageJobHandler>().WithQueueTrigger<ResolveMessageEvent>(options => {
                options.QueueName = EventNames.ResolveMessage;
                options.PollingInterval = TimeSpan.FromSeconds(5).TotalMilliseconds;
                options.InstanceCount = 1;
            })
            .AddJob<SendPushNotificationJobHandler>().WithQueueTrigger<SendPushNotificationEvent>(options => {
                options.QueueName = EventNames.SendPushNotification;
                options.PollingInterval = TimeSpan.FromSeconds(5).TotalMilliseconds;
                options.InstanceCount = 1;
            });
            var serviceProvider = workerHostBuilder.Services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            Action<DbContextOptionsBuilder> sqlServerConfiguration = (builder) => builder.UseSqlServer(configuration.GetConnectionString("CampaignsDbConnection"));
            workerHostBuilder.Services.AddDbContext<CampaignsDbContext>(options.ConfigureDbContext ?? sqlServerConfiguration);
            workerHostBuilder.Services.TryAddTransient<Func<string, IPushNotificationService>>(serviceProvider => key => new PushNotificationServiceNoop());
            workerHostBuilder.Services.TryAddTransient<Func<string, IEventDispatcher>>(serviceProvider => key => new EventDispatcherNoop());
            workerHostBuilder.Services.TryAddTransient<IContactResolver, ContactResolverNoop>();
            workerHostBuilder.Services.TryAddTransient<IDistributionListService, DistributionListService>();
            workerHostBuilder.Services.TryAddTransient<IMessageService, MessageService>();
            workerHostBuilder.Services.TryAddTransient<IContactService, ContactService>();
            workerHostBuilder.Services.TryAddSingleton(new DatabaseSchemaNameResolver(options.DatabaseSchema));
            return workerHostBuilder;
        }

        /// <summary>
        /// Adds an Azure specific implementation of <see cref="IPushNotificationService"/> for sending push notifications.
        /// </summary>
        /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
        /// <param name="configure">Configure the available options for push notifications. Null to use defaults.</param>
        public static void UsePushNotificationServiceAzure(this CampaignsJobsOptions options, Action<IServiceProvider, PushNotificationAzureOptions> configure = null) =>
            options.Services.AddPushNotificationServiceAzure(KeyedServiceNames.PushNotificationServiceKey, configure);

        /// <summary>
        /// Adds <see cref="IEventDispatcher"/> using Indice worker host as a queuing mechanism.
        /// </summary>
        /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
        public static void UseEventDispatcherHosting(this CampaignsJobsOptions options) =>
            options.Services.AddKeyedService<IEventDispatcher, EventDispatcherHosting, string>(
                key: KeyedServiceNames.EventDispatcherServiceKey,
                serviceProvider => new EventDispatcherHosting(new MessageQueueFactory(serviceProvider)),
                serviceLifetime: ServiceLifetime.Transient
            );

        /// <summary>
        /// Configures that campaign contact information will be resolved by contacting the Identity Server instance. 
        /// </summary>
        /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
        /// <param name="configure">Delegate used to configure <see cref="ContactResolverIdentity"/> service.</param>
        public static void UseIdentityContactResolver(this CampaignsJobsOptions options, Action<ContactResolverIdentityOptions> configure) {
            var serviceOptions = new ContactResolverIdentityOptions();
            configure.Invoke(serviceOptions);
            options.Services.Configure<ContactResolverIdentityOptions>(config => {
                config.BaseAddress = serviceOptions.BaseAddress;
                config.ClientId = serviceOptions.ClientId;
                config.ClientSecret = serviceOptions.ClientSecret;
            });
            options.Services.AddDistributedMemoryCache();
            options.Services.AddHttpClient<IContactResolver, ContactResolverIdentity>(httpClient => {
                httpClient.BaseAddress = serviceOptions.BaseAddress;
            });
        }

        /// <summary>
        /// Adds a custom contact resolver that discovers contact information from a third-party system.
        /// </summary>
        /// <typeparam name="TContactResolver">The concrete type of <see cref="IContactResolver"/>.</typeparam>
        /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
        public static void UseContactResolver<TContactResolver>(this CampaignsJobsOptions options) where TContactResolver : IContactResolver =>
            options.Services.AddTransient(typeof(IContactResolver), typeof(TContactResolver));
    }
}
