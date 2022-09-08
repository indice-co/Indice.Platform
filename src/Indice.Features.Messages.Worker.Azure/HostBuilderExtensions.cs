using System.Security.Claims;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Indice.Features.Messages.Core.Manager;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;
using Indice.Features.Messages.Worker.Azure;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>Extension methods on <see cref="IHostBuilder"/> used to configure Azure Functions for campaign management system.</summary>
    public static class HostBuilderExtensions
    {
        /// <summary>Configures services used by the queue triggers used for campaign management system.</summary>
        /// <param name="hostBuilder">A program initialization abstraction.</param>
        /// <param name="configure">Configure action for <see cref="MessageOptions"/>.</param>
        public static IHostBuilder ConfigureMessageFunctions(this IHostBuilder hostBuilder, Action<IConfiguration, IHostEnvironment, MessageOptions> configure = null) {
            hostBuilder.ConfigureServices((hostBuilderContext, services) => {
                var options = new MessageOptions {
                    Services = services
                };
                configure?.Invoke(hostBuilderContext.Configuration, hostBuilderContext.HostingEnvironment, options);
                services.AddCoreServices(options, hostBuilderContext.Configuration);
                services.AddJobHandlerServices();
            });
            return hostBuilder;
        }

        /// <summary>Configures services used by the queue triggers used for campaign management system.</summary>
        /// <param name="hostBuilder">A program initialization abstraction.</param>
        /// <param name="configure">Configure action for <see cref="MessageOptions"/>.</param>
        public static IHostBuilder ConfigureMessageFunctions(this IHostBuilder hostBuilder, Action<IConfiguration, MessageOptions> configure = null) {
            hostBuilder.ConfigureServices((hostBuilderContext, services) => {
                var options = new MessageOptions {
                    Services = services
                };
                configure?.Invoke(hostBuilderContext.Configuration, options);
                services.AddCoreServices(options, hostBuilderContext.Configuration);
                services.AddJobHandlerServices();
            });
            return hostBuilder;
        }

        private static IServiceCollection AddCoreServices(this IServiceCollection services, MessageOptions options, IConfiguration configuration) {
            services.TryAddTransient<Func<string, IPushNotificationService>>(serviceProvider => key => new PushNotificationServiceNoop());
            services.TryAddTransient<Func<string, IEventDispatcher>>(serviceProvider => key => new EventDispatcherNoop());
            services.TryAddTransient<IEmailService, EmailServiceNoop>();
            services.TryAddTransient<IContactResolver, ContactResolverNoop>();
            Action<IServiceProvider, DbContextOptionsBuilder> sqlServerConfiguration = (serviceProvider, builder) => builder.UseSqlServer(configuration.GetConnectionString("MessagesDb"));
            services.AddDbContext<CampaignsDbContext>(options.ConfigureDbContext ?? sqlServerConfiguration);
            services.TryAddTransient<IDistributionListService, DistributionListService>();
            services.TryAddTransient<IMessageService, MessageService>();
            services.TryAddTransient<IContactService, ContactService>();
            services.TryAddTransient<ICampaignService, CampaignService>();
            services.TryAddTransient<IMessageTypeService, MessageTypeService>();
            services.TryAddTransient<ITemplateService, TemplateService>();
            services.TryAddTransient<CreateCampaignRequestValidator>();
            services.TryAddTransient<CreateMessageTypeRequestValidator>();
            services.TryAddTransient<NotificationsManager>();
            services.TryAddSingleton(new DatabaseSchemaNameResolver(options.DatabaseSchema));
            services.TryAddTransient<IUserNameAccessor>(serviceProvider => new UserNameStaticAccessor("worker"));
            return services;
        }

        private static IServiceCollection AddJobHandlerServices(this IServiceCollection services) {
            services.TryAddTransient<ICampaignJobHandler<CampaignCreatedEvent>, CampaignCreatedHandler>();
            services.TryAddTransient<ICampaignJobHandler<ResolveMessageEvent>, ResolveMessageHandler>();
            services.TryAddTransient<ICampaignJobHandler<SendPushNotificationEvent>, SendPushNotificationHandler>();
            services.TryAddTransient<ICampaignJobHandler<SendEmailEvent>, SendEmailHandler>();
            services.TryAddTransient<ICampaignJobHandler<SendSmsEvent>, SendSmsHandler>();
            services.AddTransient<MessageJobHandlerFactory>();
            return services;
        }

        /// <summary>Adds an Azure specific implementation of <see cref="IPushNotificationService"/> for sending push notifications.</summary>
        /// <param name="options">Options used when configuring campaign Azure Functions.</param>
        /// <param name="configure">Configure the available options for push notifications. Null to use defaults.</param>
        public static MessageOptions UsePushNotificationServiceAzure(this MessageOptions options, Action<IServiceProvider, PushNotificationAzureOptions> configure = null) {
            options.Services.AddPushNotificationServiceAzure(KeyedServiceNames.PushNotificationServiceKey, configure);
            return options;
        }

        /// <summary>Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.</summary>
        /// <param name="options">Options used when configuring campaign Azure Functions.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static MessageOptions UseEventDispatcherAzure(this MessageOptions options, Action<IServiceProvider, MessageEventDispatcherAzureOptions> configure = null) {
            options.Services.AddEventDispatcherAzure(KeyedServiceNames.EventDispatcherServiceKey, (serviceProvider, options) => {
                var eventDispatcherOptions = new MessageEventDispatcherAzureOptions {
                    ConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(EventDispatcherAzure.CONNECTION_STRING_NAME),
                    Enabled = true,
                    EnvironmentName = serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName,
                    ClaimsPrincipalSelector = ClaimsPrincipal.ClaimsPrincipalSelector ?? (() => ClaimsPrincipal.Current)
                };
                configure?.Invoke(serviceProvider, eventDispatcherOptions);
                options.ClaimsPrincipalSelector = eventDispatcherOptions.ClaimsPrincipalSelector;
                options.ConnectionString = eventDispatcherOptions.ConnectionString;
                options.Enabled = eventDispatcherOptions.Enabled;
                options.EnvironmentName = eventDispatcherOptions.EnvironmentName;
                options.QueueMessageEncoding = eventDispatcherOptions.QueueMessageEncoding;
                options.TenantIdSelector = eventDispatcherOptions.TenantIdSelector;
                options.UseCompression = true;
            });
            return options;
        }

        /// <summary>Adds an instance of <see cref="IEmailService"/> using SMTP settings in configuration.</summary>
        /// <param name="options">Options used when configuring messages in Azure Functions.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static MessageOptions UseEmailServiceSmtp(this MessageOptions options, IConfiguration configuration) {
            options.Services.AddEmailServiceSmtp(configuration);
            return options;
        }

        /// <summary>Adds an instance of <see cref="IEmailService"/> that uses SparkPost to send emails.</summary>
        /// <param name="options">Options used when configuring messages in Azure Functions.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static MessageOptions UseEmailServiceSparkpost(this MessageOptions options, IConfiguration configuration) {
            options.Services.AddEmailServiceSparkpost(configuration);
            return options;
        }

        /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto.</summary>
        /// <param name="options">Options used when configuring messages in Azure Functions.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static MessageOptions UseSmsServiceYuboto(this MessageOptions options, IConfiguration configuration) {
            options.Services.AddSmsServiceYuboto(configuration);
            return options;
        }

        /// <summary>Adds an instance of <see cref="ISmsService"/> using Apifon.</summary>
        /// <param name="options">Options used when configuring messages in Azure Functions.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static MessageOptions UseSmsServiceApifon(this MessageOptions options, IConfiguration configuration, Action<SmsServiceApifonOptions> configure = null) {
            options.Services.AddSmsServiceApifon(configuration, configure);
            return options;
        }

        /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto.</summary>
        /// <param name="options">Options used when configuring messages in Azure Functions.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static MessageOptions UseSmsServiceViber(this MessageOptions options, IConfiguration configuration) {
            options.Services.AddSmsServiceViber(configuration);
            return options;
        }

        /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto Omni for sending Viber messages.</summary>
        /// <param name="options">Options used when configuring messages in Azure Functions.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static MessageOptions UseViberServiceYubotoOmni(this MessageOptions options, IConfiguration configuration) {
            options.Services.AddViberServiceYubotoOmni(configuration);
            return options;
        }

        /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto Omni from sending regular SMS messages.</summary>
        /// <param name="options">Options used when configuring messages in Azure Functions.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static MessageOptions UseSmsServiceYubotoOmni(this MessageOptions options, IConfiguration configuration) {
            options.Services.AddSmsServiceYubotoOmni(configuration);
            return options;
        }

        /// <summary>Configures that campaign contact information will be resolved by contacting the Identity Server instance.</summary>
        /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
        /// <param name="configure">Delegate used to configure <see cref="ContactResolverIdentity"/> service.</param>
        public static MessageOptions UseIdentityContactResolver(this MessageOptions options, Action<ContactResolverIdentityOptions> configure) {
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
            return options;
        }

        /// <summary>Adds a custom contact resolver that discovers contact information from a third-party system.</summary>
        /// <typeparam name="TContactResolver">The concrete type of <see cref="IContactResolver"/>.</typeparam>
        /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
        public static MessageOptions UseContactResolver<TContactResolver>(this MessageOptions options) where TContactResolver : IContactResolver {
            options.Services.AddTransient(typeof(IContactResolver), typeof(TContactResolver));
            return options;
        }
    }
}
