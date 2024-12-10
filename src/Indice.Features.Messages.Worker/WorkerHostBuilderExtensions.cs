using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Indice.Features.Messages.Core.Hosting;
using Indice.Features.Messages.Core.Manager;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;
using Indice.Features.Messages.Worker;
using Indice.Features.Messages.Worker.Handlers;
using Indice.Hosting;
using Indice.Hosting.Services;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="WorkerHostBuilder"/> type.</summary>
public static class WorkerHostBuilderExtensions
{
    /// <summary>Adds the job handlers required the for campaigns feature.</summary>
    /// <param name="workerHostBuilder">A helper class to configure the worker host.</param>
    /// <param name="configure"></param>
    /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
    public static WorkerHostBuilder AddMessageJobs(this WorkerHostBuilder workerHostBuilder, Action<MessageJobsOptions>? configure = null) {
        var serviceProvider = workerHostBuilder.Services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var options = new MessageJobsOptions {
            Services = workerHostBuilder.Services,
            Configuration = configuration
        };
        configure?.Invoke(options);
        workerHostBuilder.AddJobHandlers(options);
        workerHostBuilder.Services.AddCoreServices(options, configuration);
        workerHostBuilder.Services.AddJobHandlerServices();
        workerHostBuilder.Services.Configure<HostOptions>(hostOptions => {
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling
            hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
        });
        workerHostBuilder.Services.Configure<MessageWorkerOptions>(messageWorkerOptions => {
            messageWorkerOptions.ContactRetainPeriodInDays = options.ContactRetainPeriodInDays;
        });
        workerHostBuilder.Services.AddHostedService<StartupSeedHostedService>();
        return workerHostBuilder;
    }

    private static void AddJobHandlers(this WorkerHostBuilder workerHostBuilder, MessageJobsOptions messageOptions) {
        var random = new Random();
        workerHostBuilder.AddJob<CampaignPublishedJobHandler>().WithQueueTrigger<CampaignCreatedEvent>(options => {
            options.QueueName = EventNames.CampaignCreated;
            options.PollingInterval = random.Next((int)messageOptions.QueuePollingInterval, (int)messageOptions.QueuePollingInterval + 200);
            options.MaxPollingInterval = options.PollingInterval + messageOptions.QueueMaxPollingInterval;
            options.InstanceCount = 1;
        })
        .AddJob<ResolveMessageJobHandler>().WithQueueTrigger<ResolveMessageEvent>(options => {
            options.QueueName = EventNames.ResolveMessage;
            options.PollingInterval = random.Next((int)messageOptions.QueuePollingInterval, (int)messageOptions.QueuePollingInterval + 200);
            options.MaxPollingInterval = options.PollingInterval + messageOptions.QueueMaxPollingInterval;
            options.InstanceCount = 1;
        })
        .AddJob<SendPushNotificationJobHandler>().WithQueueTrigger<SendPushNotificationEvent>(options => {
            options.QueueName = EventNames.SendPushNotification;
            options.PollingInterval = random.Next((int)messageOptions.QueuePollingInterval, (int)messageOptions.QueuePollingInterval + 200);
            options.MaxPollingInterval = options.PollingInterval + messageOptions.QueueMaxPollingInterval;
            options.InstanceCount = 1;
        })
        .AddJob<SendEmailJobHandler>().WithQueueTrigger<SendEmailEvent>(options => {
            options.QueueName = EventNames.SendEmail;
            options.PollingInterval = random.Next((int)messageOptions.QueuePollingInterval, (int)messageOptions.QueuePollingInterval + 200);
            options.MaxPollingInterval = options.PollingInterval + messageOptions.QueueMaxPollingInterval;
            options.InstanceCount = 1;
        })
        .AddJob<SendSmsJobHandler>().WithQueueTrigger<SendSmsEvent>(options => {
            options.QueueName = EventNames.SendSms;
            options.PollingInterval = random.Next((int)messageOptions.QueuePollingInterval, (int)messageOptions.QueuePollingInterval + 200);
            options.MaxPollingInterval = options.PollingInterval + messageOptions.QueueMaxPollingInterval;
            options.InstanceCount = 1;
        });
    }

    private static void AddJobHandlerServices(this IServiceCollection services) {
        services.TryAddTransient<ICampaignJobHandler<CampaignCreatedEvent>, CampaignCreatedHandler>();
        services.TryAddTransient<ICampaignJobHandler<ResolveMessageEvent>, ResolveMessageHandler>();
        services.TryAddTransient<ICampaignJobHandler<SendPushNotificationEvent>, SendPushNotificationHandler>();
        services.TryAddTransient<ICampaignJobHandler<SendEmailEvent>, SendEmailHandler>();
        services.TryAddTransient<ICampaignJobHandler<SendSmsEvent>, SendSmsHandler>();
        services.AddTransient<MessageJobHandlerFactory>();
    }

    private static void AddCoreServices(this IServiceCollection services, MessageJobsOptions options, IConfiguration configuration) {
        options.Services.TryAddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
        options.Services.AddKeyedTransient<IEventDispatcher, EventDispatcherHosting>(
            serviceKey: KeyedServiceNames.EventDispatcherServiceKey,
            (serviceProvider, key) => new EventDispatcherHosting(new MessageQueueFactory(serviceProvider))
        );
        services.AddEmailServiceNoop();
        services.AddPushNotificationServiceNoop();
        services.AddSmsServiceNoop();
        services.TryAddSingleton<IContactResolver, ContactResolverNoop>();
        Action<DbContextOptionsBuilder> sqlServerConfiguration = (builder) => builder.UseSqlServer(configuration.GetConnectionString("MessagesDb"));
        services.AddDbContext<CampaignsDbContext>(options.ConfigureDbContext ?? sqlServerConfiguration);
        services.TryAddTransient<IDistributionListService, DistributionListService>();
        services.TryAddTransient<IMessageService, MessageService>();
        services.TryAddTransient<IContactService, ContactService>();
        services.TryAddTransient<ICampaignService, CampaignService>();
        services.TryAddTransient<ICampaignAttachmentService, CampaignAttachmentService>();
        services.TryAddTransient<IMessageTypeService, MessageTypeService>();
        services.TryAddTransient<IMessageSenderService, MessageSenderService>();
        services.TryAddTransient<ITemplateService, TemplateService>();
        services.TryAddTransient<CreateCampaignRequestValidator>();
        services.TryAddTransient<CreateMessageTypeRequestValidator>();
        services.TryAddTransient<NotificationsManager>();
        services.TryAddSingleton(new DatabaseSchemaNameResolver(options.DatabaseSchema));
        services.AddScoped<IUserNameAccessor>(serviceProvider => new UserNameStaticAccessor("worker"));
    }

    /// <summary>Adds <see cref="IFileService"/> using local file system as the backing store.</summary>
    /// <param name="options">Options used to configure the Campaigns API feature.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static void UseFilesLocal(this MessageJobsOptions options, Action<FileServiceLocalOptions>? configure = null) =>
        options.Services.AddFiles(options => options.AddFileSystem(KeyedServiceNames.FileServiceKey, configure));

    /// <summary>Adds <see cref="IFileService"/> using Azure Blob Storage as the backing store.</summary>
    /// <param name="options">Options used to configure the Campaigns API feature.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static void UseFilesAzure(this MessageJobsOptions options, Action<FileServiceAzureOptions>? configure = null) =>
        options.Services.AddFiles(options => options.AddAzureStorage(KeyedServiceNames.FileServiceKey, configure));

    /// <summary>Adds an Azure specific implementation of <see cref="IPushNotificationService"/> for sending push notifications.</summary>
    /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
    /// <param name="configure">Configure the available options for push notifications. Null to use defaults.</param>
    public static MessageJobsOptions UsePushNotificationServiceAzure(this MessageJobsOptions options, Action<IServiceProvider, PushNotificationAzureOptions>? configure = null) {
        options.Services.AddPushNotificationServiceAzure(KeyedServiceNames.PushNotificationServiceKey, configure);
        return options;
    }

    /// <summary>Adds an instance of <see cref="IEmailService"/> using SMTP settings in configuration.</summary>
    /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static MessageJobsOptions UseEmailServiceSmtp(this MessageJobsOptions options, IConfiguration configuration) {
        options.Services.AddEmailServiceSmtp(configuration);
        options.Services.AddSingleton(serviceProvider => {
            var smtpSettings = serviceProvider.GetRequiredService<IOptions<EmailServiceSettings>>().Value;
            return new Func<EmailProviderInfo>(() => new EmailProviderInfo(smtpSettings.Sender, smtpSettings.SenderName));
        });
        return options;
    }

    /// <summary>Adds an instance of <see cref="IEmailService"/> that uses SparkPost to send emails.</summary>
    /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static MessageJobsOptions UseEmailServiceSparkPost(this MessageJobsOptions options, IConfiguration configuration) {
        options.Services.AddEmailServiceSparkPost(configuration);
        options.Services.AddSingleton(serviceProvider => {
            var sparkPostSettings = serviceProvider.GetRequiredService<IOptions<EmailServiceSparkPostSettings>>().Value;
            return new Func<EmailProviderInfo>(() => new EmailProviderInfo(sparkPostSettings.Sender, sparkPostSettings.SenderName));
        });
        return options;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto.</summary>
    /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static MessageJobsOptions UseSmsServiceYuboto(this MessageJobsOptions options, IConfiguration configuration) {
        options.Services.AddSmsServiceYuboto(configuration);
        return options;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Apifon.</summary>
    /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static MessageJobsOptions UseSmsServiceApifon(this MessageJobsOptions options, IConfiguration configuration, Action<SmsServiceApifonOptions>? configure = null) {
        options.Services.AddSmsServiceApifon(configuration, configure);
        return options;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto Omni for sending Viber messages.</summary>
    /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static MessageJobsOptions UseViberServiceYubotoOmni(this MessageJobsOptions options, IConfiguration configuration) {
        options.Services.AddViberServiceYubotoOmni(configuration);
        return options;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto Omni from sending regular SMS messages.</summary>
    /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static MessageJobsOptions UseSmsServiceYubotoOmni(this MessageJobsOptions options, IConfiguration configuration) {
        options.Services.AddSmsServiceYubotoOmni(configuration);
        return options;
    }

    /// <summary>Configures that campaign contact information will be resolved by contacting the Identity Server instance.</summary>
    /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
    /// <param name="configure">Delegate used to configure <see cref="ContactResolverIdentity"/> service.</param>
    public static MessageJobsOptions UseIdentityContactResolver(this MessageJobsOptions options, Action<ContactResolverIdentityOptions> configure) {
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
    public static MessageJobsOptions UseContactResolver<TContactResolver>(this MessageJobsOptions options) where TContactResolver : IContactResolver {
        options.Services.AddTransient(typeof(IContactResolver), typeof(TContactResolver));
        return options;
    }
}
