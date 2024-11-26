using System.Collections;
using System.Reflection;
using System.Security.Claims;
using Indice.Configuration;
using Indice.Events;
using Indice.Services;
using Indice.Services.Yuboto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions on the <see cref="IServiceCollection"/>.</summary>
public static class IndiceServicesServiceCollectionExtensions
{

    /// <summary>Adds Indice common services.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static IServiceCollection AddGeneralSettings(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<GeneralSettings>(configuration.GetSection(GeneralSettings.Name));
        services.TryAddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<GeneralSettings>>().Value);
        return services;
    }

    /// <summary>The factory that creates the default instance and configuration for <see cref="PushNotificationServiceAzure"/>.</summary>
    public static readonly Func<IServiceProvider, Action<IServiceProvider, PushNotificationAzureOptions>, PushNotificationServiceAzure> GetPushNotificationServiceAzure = (serviceProvider, configure) => {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var options = new PushNotificationAzureOptions {
            ConnectionString = configuration.GetConnectionString(PushNotificationServiceAzure.ConnectionStringName) ??
                               configuration.GetSection(PushNotificationAzureOptions.Name).GetValue<string>(nameof(PushNotificationAzureOptions.ConnectionString)),
            NotificationHubPath = configuration.GetSection(PushNotificationAzureOptions.Name).GetValue<string>(nameof(PushNotificationAzureOptions.NotificationHubPath)) ??
                                  configuration.GetValue<string>(PushNotificationServiceAzure.NotificationsHubPath)
        };
        configure?.Invoke(serviceProvider, options);
        return new PushNotificationServiceAzure(options, serviceProvider.GetRequiredService<ILoggerFactory>());
    };

    /// <summary>Adds an Azure specific implementation of <see cref="IPushNotificationService"/> for sending push notifications.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configure">Configure the available options for push notifications. Null to use defaults.</param>
    public static IServiceCollection AddPushNotificationServiceAzure(this IServiceCollection services, Action<IServiceProvider, PushNotificationAzureOptions> configure = null) {
        services.TryAddTransient<IPushNotificationServiceFactory, DefaultPushNotificationServiceFactory>();
        return services.AddTransient<IPushNotificationService>(serviceProvider => GetPushNotificationServiceAzure(serviceProvider, configure));
    }

    /// <summary>
    /// Adds an Azure specific implementation, under the specified key, of <see cref="IPushNotificationService"/> for sending push notifications.
    /// Inject <b>Func&lt;string, IPushNotificationService&gt;</b> and get the service instance by using the parameter <paramref name="name"/>.
    /// </summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="name">The key under which the specified implementation is registered.</param>
    /// <param name="configure">Configure the available options for push notifications. Null to use defaults.</param>
    public static IServiceCollection AddPushNotificationServiceAzure(this IServiceCollection services, string name, Action<IServiceProvider, PushNotificationAzureOptions> configure = null) {
        services.TryAddTransient<IPushNotificationServiceFactory, DefaultPushNotificationServiceFactory>();
        services.AddKeyedTransient<IPushNotificationService, PushNotificationServiceAzure>(serviceKey: name, implementationFactory: (serviceProvider, serviceKey) => GetPushNotificationServiceAzure(serviceProvider, configure));
        return services;
    }
    
    /// <summary>Adds an instance of <see cref="IEmailService"/> using SMTP settings in configuration.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static EmailServiceBuilder AddEmailServiceSmtp(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<EmailServiceSettings>(configuration.GetSection(EmailServiceSettings.Name));
        services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<EmailServiceSettings>>().Value);
        services.AddTransient<IEmailService, EmailServiceSmtp>();
        services.AddHtmlRenderingEngineNoop();
        return new EmailServiceBuilder(services);
    }

    /// <summary>Adds an instance of <see cref="IEmailService"/> that uses SparkPost to send emails.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static EmailServiceBuilder AddEmailServiceSparkPost(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<EmailServiceSparkPostSettings>(configuration.GetSection(EmailServiceSparkPostSettings.Name));
        services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<EmailServiceSparkPostSettings>>().Value);
        services.AddHttpClient<IEmailService, EmailServiceSparkPost>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
        services.AddHtmlRenderingEngineNoop();
        return new EmailServiceBuilder(services);
    }

    /// <summary>Adds an instance of <see cref="IEmailService"/> that uses SendGrid to send emails.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static EmailServiceBuilder AddEmailServiceSendGrid(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<EmailServiceSendGridSettings>(configuration.GetSection(EmailServiceSendGridSettings.Name));
        services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<EmailServiceSendGridSettings>>().Value);
        services.AddHttpClient<IEmailService, EmailServiceSendGrid>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
        services.AddHtmlRenderingEngineNoop();
        return new EmailServiceBuilder(services);
    }

    /// <summary>Registers a rendering engine to be used by the <see cref="IEmailService"/> implementation.</summary>
    /// <typeparam name="THtmlRenderingEngine">The concrete type of <see cref="IHtmlRenderingEngine"/> to use.</typeparam>
    /// <param name="builder">Builder class for <see cref="IEmailService"/>.</param>
    public static IServiceCollection WithHtmlRenderingEngine<THtmlRenderingEngine>(this EmailServiceBuilder builder) where THtmlRenderingEngine : IHtmlRenderingEngine {
        builder.Services.AddTransient(typeof(IHtmlRenderingEngine), typeof(THtmlRenderingEngine));
        return builder.Services;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static IServiceCollection AddSmsServiceYuboto(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<SmsServiceSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        services.AddHttpClient<ISmsService, SmsServiceYuboto>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
        return services;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Apifon SMS service gateway.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddSmsServiceApifon(this IServiceCollection services, IConfiguration configuration, Action<SmsServiceApifonOptions> configure = null) {
        services.Configure<SmsServiceApifonSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        var options = new SmsServiceApifonOptions();
        configure?.Invoke(options);
        var httpClientBuilder = services.AddHttpClient<ISmsService, SmsServiceApifon>()
                                        .ConfigureHttpClient(httpClient => {
                                            httpClient.BaseAddress = new Uri("https://ars.apifon.com/services/api/v1/sms/");
                                        });
        if (options.ConfigurePrimaryHttpMessageHandler is not null) {
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(options.ConfigurePrimaryHttpMessageHandler);
        }
        return services;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Apifon IM service gateway.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddViberServiceApifon(this IServiceCollection services, IConfiguration configuration, Action<SmsServiceApifonOptions> configure = null) {
        services.Configure<SmsServiceApifonSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        var options = new SmsServiceApifonOptions();
        configure?.Invoke(options);
        var httpClientBuilder = services
            .AddHttpClient<ISmsService, ViberServiceApifon>()
            .ConfigureHttpClient(httpClient => {
                httpClient.BaseAddress = new Uri($"{ViberServiceApifon.APIFON_BASE_URL}{ViberServiceApifon.SERVICE_ENDPOINT}");
            });
        if (options?.ConfigurePrimaryHttpMessageHandler is not null) {
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(options.ConfigurePrimaryHttpMessageHandler);
        }
        return services;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static IServiceCollection AddSmsServiceViber(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<SmsServiceViberSettings>(configuration.GetSection(SmsServiceViberSettings.Name));
        services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<SmsServiceViberSettings>>().Value);
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        services.AddHttpClient<ISmsService, SmsServiceViber>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
        return services;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto Omni from sending regular SMS messages.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static IServiceCollection AddSmsServiceYubotoOmni(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<SmsServiceSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        services.AddHttpClient<ISmsService, SmsYubotoOmniService>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
        return services;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Yuboto Omni for sending Viber messages.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static IServiceCollection AddViberServiceYubotoOmni(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<SmsServiceSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<SmsServiceSettings>>().Value);
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        services.AddHttpClient<ISmsService, ViberYubotoOmniService>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
        return services;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using KapaTEL.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddSmsServiceKapaTEL(this IServiceCollection services, IConfiguration configuration, Action<SmsServiceKapaTELSettings> configure = null) {
        services.Configure<SmsServiceKapaTELSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        var options = new SmsServiceKapaTELSettings();
        configure?.Invoke(options);
        var httpClientBuilder = services.AddHttpClient<ISmsService, SmsServiceKapaTEL>()
                                        .ConfigureHttpClient(httpClient => {
                                            httpClient.BaseAddress = new Uri("https://api2.smsmobile.gr/receiver_rest.php");
                                        })
                                        .SetHandlerLifetime(TimeSpan.FromMinutes(5));
        return services;
    }

    /// <summary>Adds an instance of <see cref="ISmsService"/> using Mstat.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddSmsServiceMstat(this IServiceCollection services, IConfiguration configuration, Action<SmsServiceMstatSettings> configure = null) {
        services.Configure<SmsServiceMstatSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        var options = new SmsServiceMstatSettings();
        configure?.Invoke(options);
        var httpClientBuilder = services.AddHttpClient<ISmsService, SmsServiceMstat>()
                                        .ConfigureHttpClient(httpClient => {
                                            httpClient.BaseAddress = new Uri("https://backend.tms.m-stat.gr/api/v1/messages");
                                        })
                                        .SetHandlerLifetime(TimeSpan.FromMinutes(5));
        return services;
    }

    /// <summary>The factory that creates the default instance and configuration for <see cref="EventDispatcherAzure"/>.</summary>
    private static readonly Func<IServiceProvider, Action<IServiceProvider, EventDispatcherAzureOptions>, EventDispatcherAzure> GetEventDispatcherAzure = (serviceProvider, configure) => {
        var options = new EventDispatcherAzureOptions {
            ConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(EventDispatcherAzure.CONNECTION_STRING_NAME),
            Enabled = true,
            EnvironmentName = serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName,
            ClaimsPrincipalSelector = ClaimsPrincipal.ClaimsPrincipalSelector ?? (() => ClaimsPrincipal.Current)
        };
        configure?.Invoke(serviceProvider, options);
        return new EventDispatcherAzure(
            options.ConnectionString,
            options.EnvironmentName,
            options.Enabled,
            options.UseCompression,
            options.QueueMessageEncoding,
            options.ClaimsPrincipalSelector,
            options.TenantIdSelector
        );
    };

    /// <summary>Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddEventDispatcherAzure(this IServiceCollection services, Action<IServiceProvider, EventDispatcherAzureOptions> configure = null) {
        services.TryAddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
        return services.AddTransient<IEventDispatcher, EventDispatcherAzure>(serviceProvider => GetEventDispatcherAzure(serviceProvider, configure));
    }

    /// <summary>Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="name">The key under which the specified implementation is registered.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddEventDispatcherAzure(this IServiceCollection services, string name, Action<IServiceProvider, EventDispatcherAzureOptions> configure = null) {
        services.TryAddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
        return services.AddKeyedTransient<IEventDispatcher, EventDispatcherAzure>(serviceKey: name, implementationFactory: (serviceProvider, serviceKey) => GetEventDispatcherAzure(serviceProvider, configure));
    }

    /// <summary>Adds <see cref="IEventDispatcher"/> using an in-memory <seealso cref="Queue"/> as a backing store.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddEventDispatcherInMemory(this IServiceCollection services) {
        services.TryAddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
        return services.AddTransient<IEventDispatcher, EventDispatcherInMemory>();
    }

    /// <summary>Registers an implementation of <see cref="ILockManager"/> that uses Microsoft Azure Blob Storage as a backing store.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddLockManagerAzure(this IServiceCollection services, Action<IServiceProvider, LockManagerAzureOptions> configure = null) {
        services.AddTransient<ILockManager, LockManagerAzure>(serviceProvider => {
            var options = new LockManagerAzureOptions {
                ConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(LockManagerAzure.CONNECTION_STRING_NAME),
                EnvironmentName = serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName
            };
            configure?.Invoke(serviceProvider, options);
            return new LockManagerAzure(options);
        });
        return services;
    }

    /// <summary>Adds a in-memory implementation of <see cref="ILockManager"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddLockManagerInMemory(this IServiceCollection services) {
        services.AddSingleton<ILockManager, LockManagerInMemory>();
        return services;
    }

    /// <summary>Registers an implementation of <see cref="IPlatformEventHandler{TEvent}"/> for the specified event type.</summary>
    /// <typeparam name="TEvent">The type of the event to handler.</typeparam>
    /// <typeparam name="TEventHandler">The handler to user for the specified event.</typeparam>
    /// <param name="services">The services available in the application.</param>
    public static IServiceCollection AddPlatformEventHandler<TEvent, TEventHandler>(this IServiceCollection services)
        where TEvent : IPlatformEvent
        where TEventHandler : class, IPlatformEventHandler<TEvent> {
        services.AddTransient(typeof(IPlatformEventHandler<TEvent>), typeof(TEventHandler));
        return services;
    }
}
