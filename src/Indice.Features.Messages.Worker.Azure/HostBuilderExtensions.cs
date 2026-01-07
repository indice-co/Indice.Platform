using System.Security.Claims;
using HandlebarsDotNet;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Indice.Features.Messages.Core.Hosting;
using Indice.Features.Messages.Core.Manager;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;
using Indice.Features.Messages.Worker.Azure;
using Indice.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Hosting;

/// <summary>Extension methods on <see cref="IHostBuilder"/> used to configure Azure Functions for campaign management system.</summary>
public static class HostBuilderExtensions
{
    /// <summary>Configures services used by the queue triggers used for campaign management system.</summary>
    /// <param name="builder">A program initialization abstraction.</param>
    /// <param name="configure">Configure action for <see cref="MessageOptions"/>.</param>
    public static IHostApplicationBuilder AddMessageFunctions(this IHostApplicationBuilder builder, Action<MessageOptions>? configure = null) {
        var options = new MessageOptions {
            Services = builder.Services
        };
        configure?.Invoke(options);
        builder.Services.AddCoreServices(options, builder.Configuration);
        builder.Services.AddJobHandlerServices();
        builder.Services.Configure<WorkerOptions>(options => {
            options.InputConverters.RegisterAt<MessagesInputConverter>(0);
        });
        builder.Services.Configure<HostOptions>(hostOptions => {
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling
            hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
        });
        builder.Services.Configure<MessageWorkerOptions>(messageWorkerOptions => {
            messageWorkerOptions.ContactRetainPeriodInDays = options.ContactRetainPeriodInDays;
        });
        builder.Services.AddHostedService<StartupSeedHostedService>();
        builder.Services.TryAddSingleton(options.FunctionDisablePredicate);
        builder.Services.AddDecorator<IFunctionMetadataProvider, ExtendedFunctionMetadataProvider>();
        return builder;
    }

    /// <summary>Configures services used by the queue triggers used for campaign management system.</summary>
    /// <param name="hostBuilder">A program initialization abstraction.</param>
    /// <param name="configure">Configure action for <see cref="MessageOptions"/>.</param>
    public static IHostBuilder ConfigureMessageFunctions(this IHostBuilder hostBuilder, Action<HostBuilderContext, MessageOptions>? configure = null) =>
        hostBuilder.ConfigureServices((hostBuilderContext, services) => {
            var options = new MessageOptions {
                Services = services
            };
            configure?.Invoke(hostBuilderContext, options);
            services.AddCoreServices(options, hostBuilderContext.Configuration);
            services.AddJobHandlerServices();
            services.Configure<WorkerOptions>(options => {
                options.InputConverters.RegisterAt<MessagesInputConverter>(0);
            });
            services.Configure<HostOptions>(hostOptions => {
                // https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling
                hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            });
            services.Configure<MessageWorkerOptions>(messageWorkerOptions => {
                messageWorkerOptions.ContactRetainPeriodInDays = options.ContactRetainPeriodInDays;
            });
            services.Configure<DatabaseCleanUpOptions>(dbCleanUpOptions => {
                dbCleanUpOptions.RetentionDaysForOther = options.DatabaseCleanUpOptions.RetentionDaysForOther;
                dbCleanUpOptions.RetentionDaysForInbox = options.DatabaseCleanUpOptions.RetentionDaysForInbox;
                dbCleanUpOptions.DeletionBatchSize = options.DatabaseCleanUpOptions.DeletionBatchSize;
                dbCleanUpOptions.Enabled = options.DatabaseCleanUpOptions.Enabled;
            });
            services.AddHostedService<StartupSeedHostedService>();
            services.TryAddSingleton(options.FunctionDisablePredicate);
            services.AddDecorator<IFunctionMetadataProvider, ExtendedFunctionMetadataProvider>();

        });

    private static IServiceCollection AddCoreServices(this IServiceCollection services, MessageOptions options, IConfiguration configuration) {
        services.AddPushNotificationServiceNoop();
        services.TryAddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
        services.TryAddTransient<IFileServiceFactory, DefaultFileServiceFactory>();
        services.TryAddTransient<IEmailService, EmailServiceNoop>();
        services.TryAddTransient<IContactResolver, ContactResolverNoop>();
        Action<IServiceProvider, DbContextOptionsBuilder> sqlServerConfiguration = (serviceProvider, builder) => builder.UseSqlServer(configuration.GetConnectionString("MessagesDb"));
        services.AddDbContext<CampaignsDbContext>(options.ConfigureDbContext ?? sqlServerConfiguration);
        services.TryAddTransient<IDistributionListService, DistributionListService>();
        services.TryAddTransient<IMessageService, MessageService>();
        services.TryAddTransient<IMessageEventService, MessageEventService>();
        services.TryAddTransient<IContactService, ContactService>();
        services.TryAddTransient<ICampaignService, CampaignService>();
        services.TryAddTransient<ICampaignAttachmentService, CampaignAttachmentService>();
        services.TryAddTransient<IMessageTypeService, MessageTypeService>();
        services.TryAddTransient<ITemplateService, TemplateService>();
        services.TryAddTransient<IMessageSenderService, MessageSenderService>();
        services.TryAddTransient<CreateCampaignRequestValidator>();
        services.TryAddTransient<CreateMessageTypeRequestValidator>();
        services.TryAddTransient<NotificationsManager>();
        services.TryAddTransient<IDatabaseCleanUpService, DatabaseCleanUpService>();
        services.TryAddSingleton(new DatabaseSchemaNameResolver(options.DatabaseSchema));
        services.AddScoped<IUserNameAccessor>(serviceProvider => new UserNameStaticAccessor("worker"));
        services.TryAddScoped<UserNameAccessorAggregate>();

        services.Configure<AnalyticsOptions>(opt => {
            opt.Enabled = options.CampaignStatisticOptions.Enabled;
        });
        services.AddSingleton<MessageEventQueue>();
        services.AddSingleton<IHostedService, MessageEventHostedServcie>();
        return services;
    }

    private static IServiceCollection AddJobHandlerServices(this IServiceCollection services) {
        services.TryAddTransient<ICampaignJobHandler<CampaignCreatedEvent>, CampaignCreatedHandler>();
        services.TryAddTransient<ICampaignJobHandler<ResolveMessageEvent>, ResolveMessageHandler>();
        services.TryAddTransient<ICampaignJobHandler<SendPushNotificationEvent>, SendPushNotificationHandler>();
        services.TryAddTransient<ICampaignJobHandler<SendEmailEvent>, SendEmailHandler>();
        services.TryAddTransient<ICampaignJobHandler<SendSmsEvent>, SendSmsHandler>();
        services.TryAddTransient<ICampaignJobHandler<MarkMessagesReadEvent>, MarkReadEventHandler>();
        services.TryAddTransient<ICampaignJobHandler<MarkMessagesUnreadEvent>, MarkUnreadEventHandler>();
        services.TryAddTransient<ICampaignJobHandler<DatabaseCleanUpTimerEvent>, DatabaseCleanUpHandler>();
        services.AddTransient<MessageJobHandlerFactory>();
        return services;
    }

    /// <summary>Adds an Azure specific implementation of <see cref="IPushNotificationService"/> for sending push notifications.</summary>
    /// <param name="options">Options used when configuring campaign Azure Functions.</param>
    /// <param name="configure">Configure the available options for push notifications. Null to use defaults.</param>
    public static MessageOptions UsePushNotificationServiceAzure(this MessageOptions options, Action<IServiceProvider, PushNotificationAzureOptions>? configure = null) {
        options.Services.AddPushNotificationServiceAzure(KeyedServiceNames.PushNotificationServiceKey, configure);
        return options;
    }

    /// <summary>Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.</summary>
    /// <param name="options">Options used when configuring campaign Azure Functions.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static MessageOptions UseEventDispatcherAzure(this MessageOptions options, Action<IServiceProvider, MessageEventDispatcherAzureOptions>? configure = null) {
        options.Services.AddEventDispatcherAzure(KeyedServiceNames.EventDispatcherServiceKey, (serviceProvider, options) => {
            var eventDispatcherOptions = new MessageEventDispatcherAzureOptions {
                ConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(EventDispatcherAzure.CONNECTION_STRING_NAME),
                Enabled = true,
                EnvironmentName = serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName,
                ClaimsPrincipalSelector = ClaimsPrincipal.ClaimsPrincipalSelector ?? (() => ClaimsPrincipal.Current!)
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

    /// <summary>Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.</summary>
    /// <param name="options">Options used to configure the Campaigns API feature.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static MessageOptions UseEventDispatcherAzureServiceBus(this MessageOptions options, Action<IServiceProvider, MessageEventDispatcherAzureOptions>? configure = null) {
        options.Services!.AddEventDispatcherAzureServiceBus(Indice.Features.Messages.Core.KeyedServiceNames.EventDispatcherServiceKey,
            (serviceProvider, options) => {
                var eventDispatcherOptions = new MessageEventDispatcherAzureOptions {
                    ConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(EventDispatcherAzureServiceBus.CONNECTION_STRING_NAME),
                    Enabled = true,
                    EnvironmentName = serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName,
                    ClaimsPrincipalSelector = ClaimsPrincipal.ClaimsPrincipalSelector ?? (() => ClaimsPrincipal.Current!)
                };
                configure?.Invoke(serviceProvider, eventDispatcherOptions);
                options.ClaimsPrincipalSelector = eventDispatcherOptions.ClaimsPrincipalSelector;
                options.ConnectionString = eventDispatcherOptions.ConnectionString;
                options.Enabled = eventDispatcherOptions.Enabled;
                options.EnvironmentName = eventDispatcherOptions.EnvironmentName;
                options.TenantIdSelector = eventDispatcherOptions.TenantIdSelector;
                options.UseCompression = false;
            });
        return options;
    }

    /// <summary>Adds <see cref="IFileService"/> using local file system as the backing store.</summary>
    /// <param name="options">Options used to configure the Campaigns API feature.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static MessageOptions UseFilesLocal(this MessageOptions options, Action<FileServiceLocalOptions>? configure = null) {
        options.Services.AddFiles(options => options.AddFileSystem(KeyedServiceNames.FileServiceKey, configure));
        return options;
    }

    /// <summary>Adds <see cref="IFileService"/> using Azure Blob Storage as the backing store.</summary>
    /// <param name="options">Options used to configure the Campaigns API feature.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static MessageOptions UseFilesAzure(this MessageOptions options, Action<FileServiceAzureOptions>? configure = null) {
        void defaultConfigureAction(FileServiceAzureOptions options) {
            options.ContainerName = string.IsNullOrEmpty(options.ContainerName) ? "messaging" : $"{options.ContainerName}-messaging";
            configure?.Invoke(options);
        }
        options.Services.AddFiles(options => options.AddAzureStorage(KeyedServiceNames.FileServiceKey, defaultConfigureAction));
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
    public static MessageOptions UseEmailServiceSparkPost(this MessageOptions options, IConfiguration configuration) {
        options.Services.AddEmailServiceSparkPost(configuration);
        return options;
    }

    /// <summary>Adds an instance of <see cref="IEmailService"/> that uses Brevo to send emails.</summary>
    /// <param name="options">Options used when configuring messages in Azure Functions.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static MessageOptions UseEmailServiceBrevo(this MessageOptions options, IConfiguration configuration) {
        options.Services.AddEmailServiceBrevo(configuration);
        return options;
    }

    /// <summary>Adds an instance of <see cref="IEmailService"/> that uses SendGrid to send emails.</summary>
    /// <param name="options">Options used when configuring messages in Azure Functions.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static MessageOptions UseEmailServiceSendGrid(this MessageOptions options, IConfiguration configuration) {
        options.Services.AddEmailServiceSendGrid(configuration);
        return options;
    }

    /// <summary>Adds an instance of <see cref="IEmailService"/> according to <seealso cref="IConfiguration"/> and the <strong>Email:Provider</strong> setting.</summary>
    /// <param name="options">Options used when configuring messages in Azure Functions.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <remarks>Auto discovers the correct service to register according to configuration</remarks>    
    public static MessageOptions UseEmailService(this MessageOptions options, IConfiguration configuration) {
        options.Services.AddEmailService(configuration);
        return options;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> according to the <strong>Sms:Provider</strong> key.</summary>
    /// <param name="options">Options used when configuring messages in Azure Functions.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <remarks>Auto discovers the correct service to register according to configuration</remarks>    
    public static MessageOptions UseSmsService(this MessageOptions options, IConfiguration configuration) {
        options.Services.AddSmsService(configuration);
        return options;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Apifon.</summary>
    /// <param name="options">Options used when configuring messages in Azure Functions.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static MessageOptions UseSmsServiceApifon(this MessageOptions options, IConfiguration configuration, Action<SmsServiceApifonOptions>? configure = null) {
        options.Services.AddSmsServiceApifon(configuration, configure);
        return options;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto Omni for sending Viber messages.</summary>
    /// <param name="options">Options used when configuring messages in Azure Functions.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static MessageOptions UseSmsServiceApifonIM(this MessageOptions options, IConfiguration configuration, Action<SmsServiceApifonOptions>? configure = null) {
        options.Services.AddSmsServiceApifonIM(configuration, configure);
        return options;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto Omni for sending Viber messages.</summary>
    /// <param name="options">Options used when configuring messages in Azure Functions.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static MessageOptions UseSmsServiceYubotoOmniViber(this MessageOptions options, IConfiguration configuration) {
        options.Services.AddSmsServiceYubotoOmniViber(configuration);
        return options;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto Omni for sending regular SMS messages.</summary>
    /// <param name="options">Options used when configuring messages in Azure Functions.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static MessageOptions UseSmsServiceYubotoOmni(this MessageOptions options, IConfiguration configuration) {
        options.Services.AddSmsServiceYubotoOmni(configuration);
        return options;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Vonage for sending regular SMS messages.</summary>
    /// <param name="options">Options used when configuring messages in Azure Functions.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static MessageOptions UseSmsServiceVonage(this MessageOptions options, IConfiguration configuration) {
        options.Services.AddSmsServiceVonage(configuration);
        return options;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Twilio for sending regular SMS messages.</summary>
    /// <param name="options">Options used when configuring messages in Azure Functions.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static MessageOptions UseSmsServiceTwilio(this MessageOptions options, IConfiguration configuration) {
        options.Services.AddSmsServiceTwilio(configuration);
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

    /// <summary>Configures that campaign contact information will be resolved by contacting the Identity Server instance.</summary>
    /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
    public static MessageOptions WithServiceBusTriggers(this MessageOptions options) {
        options.FunctionDisablePredicate = ExcludeQueueTriggers;
        return options;
    }


    internal static readonly ExtendedFunctionMetadataProviderDisablePredicate ExcludeQueueTriggers =
                                           ExcludeFunctions(EventNames.CampaignCreated,
                                                            EventNames.ResolveMessage,
                                                            EventNames.SendEmail,
                                                            EventNames.SendPushNotification,
                                                            EventNames.SendSms,
                                                            EventNames.MarkAllAsRead,
                                                            EventNames.MarkAllAsUnread
                                                            );

    internal static readonly ExtendedFunctionMetadataProviderDisablePredicate ExcludeServiceBusTriggers =
                                           ExcludeFunctions($"{ServiceBusTriggers.ServiceBusTriggerPrefix}{EventNames.CampaignCreated}",
                                                            $"{ServiceBusTriggers.ServiceBusTriggerPrefix}{EventNames.ResolveMessage}",
                                                            $"{ServiceBusTriggers.ServiceBusTriggerPrefix}{EventNames.SendEmail}",
                                                            $"{ServiceBusTriggers.ServiceBusTriggerPrefix}{EventNames.SendPushNotification}",
                                                            $"{ServiceBusTriggers.ServiceBusTriggerPrefix}{EventNames.SendSms}",
                                                            $"{ServiceBusTriggers.ServiceBusTriggerPrefix}{EventNames.MarkAllAsRead}",
                                                            $"{ServiceBusTriggers.ServiceBusTriggerPrefix}{EventNames.MarkAllAsUnread}");

    internal static ExtendedFunctionMetadataProviderDisablePredicate ExcludeFunctions(params string[] functionNames) {
        return (fn, Configuration) => functionNames.Any(x => x.Equals(fn.Name, StringComparison.OrdinalIgnoreCase));
    }
}
