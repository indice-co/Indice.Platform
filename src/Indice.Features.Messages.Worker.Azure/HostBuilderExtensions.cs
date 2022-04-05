using Indice.AspNetCore.Features.Campaigns.Workers.Azure;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        public static IHostBuilder ConfigureCampaigns(this IHostBuilder hostBuilder, Action<IConfiguration, CampaignsOptions> configure = null) {
            hostBuilder.ConfigureServices((hostBuilderContext, services) => {
                var options = new CampaignsOptions {
                    Services = services
                };
                configure?.Invoke(hostBuilderContext.Configuration, options);
                services.AddCoreServices(options, hostBuilderContext.Configuration);
                services.AddJobHandlerServices();
            });
            return hostBuilder;
        }

        private static IServiceCollection AddCoreServices(this IServiceCollection services, CampaignsOptions options, IConfiguration configuration) {
            services.TryAddTransient<Func<string, IPushNotificationService>>(serviceProvider => key => new PushNotificationServiceNoop());
            services.TryAddTransient<Func<string, IEventDispatcher>>(serviceProvider => key => new EventDispatcherNoop());
            Action<DbContextOptionsBuilder> sqlServerConfiguration = (builder) => builder.UseSqlServer(configuration.GetConnectionString("CampaignsDbConnection"));
            services.AddDbContext<CampaignsDbContext>(options.ConfigureDbContext ?? sqlServerConfiguration);
            services.TryAddTransient<IContactResolver, ContactResolverNoop>();
            services.TryAddTransient<IDistributionListService, DistributionListService>();
            services.TryAddTransient<IMessageService, MessageService>();
            services.TryAddTransient<IContactService, ContactService>();
            services.TryAddSingleton(new DatabaseSchemaNameResolver(options.DatabaseSchema));
            return services;
        }

        private static IServiceCollection AddJobHandlerServices(this IServiceCollection services) {
            services.TryAddTransient<ICampaignJobHandler<CampaignPublishedEvent>, CampaignPublishedHandler>();
            services.TryAddTransient<ICampaignJobHandler<ResolveMessageEvent>, ResolveMessageHandler>();
            services.TryAddTransient<ICampaignJobHandler<SendPushNotificationEvent>, SendPushNotificationHandler>();
            services.AddTransient<CampaignJobHandlerFactory>();
            return services;
        }

        /// <summary>
        /// Adds an Azure specific implementation of <see cref="IPushNotificationService"/> for sending push notifications.
        /// </summary>
        /// <param name="options">Options used when configuring campaign Azure Functions.</param>
        /// <param name="configure">Configure the available options for push notifications. Null to use defaults.</param>
        public static void UsePushNotificationServiceAzure(this CampaignsOptions options, Action<IServiceProvider, PushNotificationAzureOptions> configure = null) =>
            options.Services.AddPushNotificationServiceAzure(KeyedServiceNames.PushNotificationServiceKey, configure);

        /// <summary>
        /// Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.
        /// </summary>
        /// <param name="options">Options used when configuring campaign Azure Functions.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static void UseEventDispatcherAzure(this CampaignsOptions options, Action<IServiceProvider, EventDispatcherAzureOptions> configure = null) =>
            options.Services.AddEventDispatcherAzure(KeyedServiceNames.EventDispatcherServiceKey, configure);

        /// <summary>
        /// Configures that campaign contact information will be resolved by contacting the Identity Server instance. 
        /// </summary>
        /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
        /// <param name="configure">Delegate used to configure <see cref="ContactResolverIdentity"/> service.</param>
        public static void UseIdentityContactResolver(this CampaignsOptions options, Action<ContactResolverIdentityOptions> configure) {
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
        public static void UseContactResolver<TContactResolver>(this CampaignsOptions options) where TContactResolver : IContactResolver =>
            options.Services.AddTransient(typeof(IContactResolver), typeof(TContactResolver));
    }
}
