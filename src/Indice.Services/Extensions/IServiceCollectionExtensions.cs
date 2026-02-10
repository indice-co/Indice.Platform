using System.Collections;
using System.Security.Claims;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Indice.Configuration;
using Indice.Events;
using Indice.Services;
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
    public static readonly Func<IServiceProvider, Action<IServiceProvider, PushNotificationAzureOptions>?, PushNotificationServiceAzure> GetPushNotificationServiceAzure = (serviceProvider, configure) => {
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
    public static IServiceCollection AddPushNotificationServiceAzure(this IServiceCollection services, Action<IServiceProvider, PushNotificationAzureOptions>? configure = null) {
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
    public static IServiceCollection AddPushNotificationServiceAzure(this IServiceCollection services, string name, Action<IServiceProvider, PushNotificationAzureOptions>? configure = null) {
        services.TryAddTransient<IPushNotificationServiceFactory, DefaultPushNotificationServiceFactory>();
        services.AddKeyedTransient<IPushNotificationService, PushNotificationServiceAzure>(serviceKey: name, implementationFactory: (serviceProvider, serviceKey) => GetPushNotificationServiceAzure(serviceProvider, configure));
        return services;
    }

    /// <summary>Discovers and Adds an implementation of <see cref="IEmailService"/> according to the <seealso cref="IConfiguration"/> section <strong>Email</strong>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <remarks>Automatically discovers the correct provider using the configuration setting <strong>Email:Provider</strong> to automatically load the correct configuration.
    /// <br />Acceptable values:
    /// <strong>smtp, sparkpost, sendgrid, brevo, none</strong>
    /// </remarks>
    public static EmailServiceBuilder AddEmailService(this IServiceCollection services, IConfiguration configuration) {
        var providerNamesText = configuration.GetSection(EmailServiceSettings.Name).GetValue<string>("Provider");
        var providerNames = (providerNamesText ?? EmailServiceSmtp.ServiceName).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var name in providerNames) {
            switch (name) {
                case EmailServiceSmtp.ServiceName:
                    services.AddEmailServiceSmtp(configuration);
                    break;
                case EmailServiceSparkPost.ServiceName:
                    services.AddEmailServiceSparkPost(configuration);
                    break;
                case EmailServiceSendGrid.ServiceName:
                    services.AddEmailServiceSendGrid(configuration);
                    break;
                case EmailServiceBrevo.ServiceName:
                    services.AddEmailServiceBrevo(configuration);
                    break;
                case EmailServiceNoop.ServiceName:
                default:
                    services.AddEmailServiceNoop();
                    break;
            }
        }
        return new EmailServiceBuilder(services);
    }

    /// <summary>Adds an implementation of <see cref="IEmailService"/> using SMTP settings in configuration.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static EmailServiceBuilder AddEmailServiceSmtp(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<EmailServiceSettings>(configuration.GetSection(EmailServiceSettings.Name));
        services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<EmailServiceSettings>>().Value);
        services.AddTransient<IEmailService, EmailServiceSmtp>();
        services.AddSingleton((sp) => {
            var options = sp.GetRequiredService<IOptions<EmailServiceSettings>>().Value;
            return new EmailProvider(EmailServiceSmtp.ServiceName, new EmailSender(options.Sender!, options.SenderName));
        });
        services.TryAddTransient((serviceProvider) => new EmailProviderFinder(() => serviceProvider.GetServices<EmailProvider>().ToList()));
        services.AddHtmlRenderingEngineNoop();
        return new EmailServiceBuilder(services);
    }

    /// <summary>Adds an implementation of <see cref="IEmailService"/> that uses SparkPost to send emails.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static EmailServiceBuilder AddEmailServiceSparkPost(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<EmailServiceSparkPostSettings>(configuration.GetSection(EmailServiceSparkPostSettings.Name));
        services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<EmailServiceSparkPostSettings>>().Value);
        services.AddHttpClient<IEmailService, EmailServiceSparkPost>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
        services.AddSingleton((serviceProvider) => {
            var options = serviceProvider.GetRequiredService<IOptions<EmailServiceSparkPostSettings>>().Value;
            return new EmailProvider(EmailServiceSparkPost.ServiceName, new EmailSender(options.Sender!, options.SenderName));
        });
        services.TryAddTransient((serviceProvider) => new EmailProviderFinder(() => serviceProvider.GetServices<EmailProvider>().ToList()));
        services.AddHtmlRenderingEngineNoop();
        return new EmailServiceBuilder(services);
    }

    /// <summary>Adds an implementation of <see cref="IEmailService"/> that uses SendGrid to send emails.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static EmailServiceBuilder AddEmailServiceSendGrid(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<EmailServiceSendGridSettings>(configuration.GetSection(EmailServiceSendGridSettings.Name));
        services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<EmailServiceSendGridSettings>>().Value);
        services.AddHttpClient<IEmailService, EmailServiceSendGrid>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
        services.AddSingleton((serviceProvider) => {
            var options = serviceProvider.GetRequiredService<IOptions<EmailServiceSendGridSettings>>().Value;
            return new EmailProvider(EmailServiceSendGrid.ServiceName, new EmailSender(options.Sender!, options.SenderName));
        });
        services.TryAddTransient((serviceProvider) => new EmailProviderFinder(() => serviceProvider.GetServices<EmailProvider>().ToList()));
        services.AddHtmlRenderingEngineNoop();
        return new EmailServiceBuilder(services);
    }

    /// <summary>Adds an implementation of <see cref="IEmailService"/> that uses Brevo to send emails.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static EmailServiceBuilder AddEmailServiceBrevo(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<EmailServiceBrevoSettings>(configuration.GetSection(EmailServiceBrevoSettings.Name));
        services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<EmailServiceBrevoSettings>>().Value);
        services.AddHttpClient<IEmailService, EmailServiceBrevo>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
        services.AddSingleton((serviceProvider) => {
            var options = serviceProvider.GetRequiredService<IOptions<EmailServiceBrevoSettings>>().Value;
            return new EmailProvider(EmailServiceBrevo.ServiceName, new EmailSender(options.Sender!, options.SenderName));
        });
        services.TryAddTransient((serviceProvider) => new EmailProviderFinder(() => serviceProvider.GetServices<EmailProvider>().ToList()));
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

    /// <summary>Discovers and Adds an implementation of <see cref="ISmsService"/> according to the <seealso cref="IConfiguration"/> section <strong>Sms</strong>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <remarks>Automatically discovers the correct provider using the configuration setting <strong>Sms:Provider</strong> to automatically load the correct configuration.
    /// <br />Acceptable values:
    /// <strong>yuboto, yuboto_viber, vonage, twilio, smsup, apifon, apifon_im, kapatel, mstat, none</strong>
    /// </remarks>
    public static IServiceCollection AddSmsService(this IServiceCollection services, IConfiguration configuration) {
        var providerNamesText = configuration.GetSection(SmsServiceSettings.Name).GetValue<string>("Provider");
        ArgumentException.ThrowIfNullOrWhiteSpace(providerNamesText, "Sms:Provider");
        var providerNames = providerNamesText.ToLowerInvariant().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var name in providerNames) { 
            switch (name) {
                case "yuboto":
                case "yuboto_omni":
                    services.AddSmsServiceYubotoOmni(configuration);
                    break;
                case "yuboto_viber":
                case "yubotoviber":
                case "yuboto_omni_viber":
                    services.AddSmsServiceYubotoOmniViber(configuration);
                    break;
                case "vonage":
                    services.AddSmsServiceVonage(configuration);
                    break;
                case "twilio":
                    services.AddSmsServiceTwilio(configuration);
                    break;
                case "smsup":
                    services.AddSmsServiceSmsUp(configuration);
                    break;
                case "apifon":
                    services.AddSmsServiceApifon(configuration);
                    break;
                case "apifon_im":
                case "apifonim":
                    services.AddSmsServiceApifonIM(configuration);
                    break;
                case "kapatel":
                case "kapa_tel":
                    services.AddSmsServiceApifonIM(configuration);
                    break;
                case "mstat":
                    services.AddSmsServiceMstat(configuration);
                    break;
                case "noop":
                case "none":
                    services.AddSmsServiceNoop();
                    break;
            }
        }
        return services;
    }

    /// <summary>Adds an implementation of <see cref="ISmsService"/> using Yuboto.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    [Obsolete("This SMS service provider is obsolete. Please consider using another provider like the AddSmsServiceYubotoOmni instead.", false)]
    public static IServiceCollection AddSmsServiceYuboto(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<SmsServiceSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        services.AddHttpClient<ISmsService, SmsServiceYuboto>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
        return services;
    }

    /// <summary>Adds an implementation of <see cref="ISmsService"/> using Vonage SMS service gateway.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddSmsServiceVonage(this IServiceCollection services, IConfiguration configuration, Action<SmsServiceVonageSettings>? configure = null) {
        services.Configure<SmsServiceVonageSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        var options = new SmsServiceVonageSettings();
        configure?.Invoke(options);
        services.AddHttpClient<ISmsService, SmsServiceVonage>();
        return services;
    }

    /// <summary>Adds an implementation of <see cref="ISmsService"/> using Twilio SMS service gateway.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddSmsServiceTwilio(this IServiceCollection services, IConfiguration configuration, Action<SmsServiceTwilioSettings>? configure = null) {
        services.Configure<SmsServiceTwilioSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        var options = new SmsServiceTwilioSettings();
        configure?.Invoke(options);
        services.AddHttpClient<ISmsService, SmsServiceTwilio>();
        return services;
    }

    /// <summary>Adds an implementation of <see cref="ISmsService"/> using SmSUp SMS service gateway.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddSmsServiceSmsUp(this IServiceCollection services, IConfiguration configuration, Action<SmsServiceSmsUpSettings>? configure = null) {
        services.Configure<SmsServiceSmsUpSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        var options = new SmsServiceSmsUpSettings();
        configure?.Invoke(options);
        services.AddHttpClient<ISmsService, SmsServiceSmsUp>();
        return services;
    }

    /// <summary>Adds an implementation of <see cref="ISmsService"/> using Apifon SMS service gateway.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddSmsServiceApifon(this IServiceCollection services, IConfiguration configuration, Action<SmsServiceApifonOptions>? configure = null) {
        services.Configure<SmsServiceApifonSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        var options = new SmsServiceApifonOptions();
        configure?.Invoke(options);
        var httpClientBuilder = services.AddHttpClient<ISmsService, SmsServiceApifon>();
        if (options.ConfigurePrimaryHttpMessageHandler is not null) {
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(options.ConfigurePrimaryHttpMessageHandler);
        }
        return services;
    }

    /// <summary>Adds an implementation of <see cref="ISmsService"/> using Apifon IM service gateway.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddSmsServiceApifonIM(this IServiceCollection services, IConfiguration configuration, Action<SmsServiceApifonOptions>? configure = null) {
        services.Configure<SmsServiceApifonSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        var options = new SmsServiceApifonOptions();
        configure?.Invoke(options);
        var httpClientBuilder = services.AddHttpClient<ISmsService, SmsServiceApifonIM>();
        if (options?.ConfigurePrimaryHttpMessageHandler is not null) {
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(options.ConfigurePrimaryHttpMessageHandler);
        }
        return services;
    }

    /// <summary>Adds an implementation of <see cref="ISmsService"/> using Yuboto Omni from sending regular SMS messages.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static IServiceCollection AddSmsServiceYubotoOmni(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<SmsServiceSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        services.AddHttpClient<ISmsService, SmsServiceYubotoOmni>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
        return services;
    }

    /// <summary>Adds an implementation of <see cref="ISmsService"/> using Yuboto Omni for sending Viber messages.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static IServiceCollection AddSmsServiceYubotoOmniViber(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<SmsServiceSettings>(configuration.GetSection(SmsServiceSettings.Name));
        services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<SmsServiceSettings>>().Value);
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        services.AddHttpClient<ISmsService, SmsServiceYubotoOmniViber>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
        return services;
    }

    /// <summary>Adds an implementation of <see cref="ISmsService"/> using KapaTEL.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddSmsServiceKapaTEL(this IServiceCollection services, IConfiguration configuration, Action<SmsServiceKapaTELSettings>? configure = null) {
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

    /// <summary>Adds an implementation of <see cref="ISmsService"/> using Mstat.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddSmsServiceMstat(this IServiceCollection services, IConfiguration configuration, Action<SmsServiceMstatSettings>? configure = null) {
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
    private static readonly Func<IServiceProvider, Action<IServiceProvider, EventDispatcherAzureOptions>?, EventDispatcherAzure> GetEventDispatcherAzure = (serviceProvider, configure) => {
        var options = new EventDispatcherAzureOptions {
            ConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(EventDispatcherAzure.CONNECTION_STRING_NAME),
            Enabled = true,
            EnvironmentName = serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName,
            ClaimsPrincipalSelector = ClaimsPrincipal.ClaimsPrincipalSelector ?? (() => ClaimsPrincipal.Current!)
        };
        configure?.Invoke(serviceProvider, options);
        return new EventDispatcherAzure(
            options.ConnectionString!,
            options.EnvironmentName,
            options.Enabled,
            options.UseCompression,
            options.QueueMessageEncoding,
            options.ClaimsPrincipalSelector,
            options.TenantIdSelector!,
            serviceProvider.GetService<ILogger<EventDispatcherAzure>>()
        );
    };

    /// <summary>Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddEventDispatcherAzure(this IServiceCollection services, Action<IServiceProvider, EventDispatcherAzureOptions>? configure = null) {
        services.TryAddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
        return services.AddTransient<IEventDispatcher, EventDispatcherAzure>(serviceProvider => GetEventDispatcherAzure(serviceProvider, configure));
    }

    /// <summary>Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="name">The key under which the specified implementation is registered.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddEventDispatcherAzure(this IServiceCollection services, string name, Action<IServiceProvider, EventDispatcherAzureOptions>? configure = null) {
        services.TryAddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
        return services.AddKeyedTransient<IEventDispatcher, EventDispatcherAzure>(serviceKey: name, implementationFactory: (serviceProvider, serviceKey) => GetEventDispatcherAzure(serviceProvider, configure));
    }

    /// <summary>The factory that creates the default instance and configuration for <see cref="EventDispatcherAzure"/>.</summary>
    private static readonly Func<object?, IServiceProvider, Action<IServiceProvider, EventDispatcherAzureServiceBusOptions>?, EventDispatcherAzureServiceBus> GetEventDispatcherAzureServiceBus = (serviceKey, serviceProvider, configure) => {
        var options = new EventDispatcherAzureServiceBusOptions {
            ConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(EventDispatcherAzureServiceBus.CONNECTION_STRING_NAME),
            Enabled = true,
            EnvironmentName = serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName,
            ClaimsPrincipalSelector = ClaimsPrincipal.ClaimsPrincipalSelector ?? (() => ClaimsPrincipal.Current!)
        };
        configure?.Invoke(serviceProvider, options);
        return new EventDispatcherAzureServiceBus(
            new ServiceBusClient(connectionString: options.ConnectionString),
            options.CreateQueueIfNotExists ? new ServiceBusAdministrationClient(connectionString: options.ConnectionString) : null,
            options.EnvironmentName,
            options.Enabled,
            options.UseCompression,
            options.ClaimsPrincipalSelector,
            options.TenantIdSelector!
        );
    };

    /// <summary>Adds <see cref="IEventDispatcher"/> using Azure ServiceBus as a queuing mechanism.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddEventDispatcherAzureServiceBus(this IServiceCollection services, Action<IServiceProvider, EventDispatcherAzureServiceBusOptions>? configure = null) {
        services.TryAddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
        return services.AddSingleton<IEventDispatcher, EventDispatcherAzureServiceBus>(serviceProvider => GetEventDispatcherAzureServiceBus(null, serviceProvider, configure));
    }

    /// <summary>Adds <see cref="IEventDispatcher"/> using Azure ServiceBus as a queuing mechanism.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="name">The key under which the specified implementation is registered.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddEventDispatcherAzureServiceBus(this IServiceCollection services, string name, Action<IServiceProvider, EventDispatcherAzureServiceBusOptions>? configure = null) {
        services.TryAddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
        return services.AddKeyedSingleton<IEventDispatcher, EventDispatcherAzureServiceBus>(serviceKey: name, implementationFactory: (serviceProvider, serviceKey) => GetEventDispatcherAzureServiceBus(name, serviceProvider, configure));
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
    public static IServiceCollection AddLockManagerAzure(this IServiceCollection services, Action<IServiceProvider, LockManagerAzureOptions>? configure = null) {
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

    /// <summary>Try to register an implementation of <see cref="IPlatformEventHandler{TEvent}"/> for the specified event type if not already exists.</summary>
    /// <typeparam name="TEvent">The type of the event to handler.</typeparam>
    /// <typeparam name="TEventHandler">The handler to user for the specified event.</typeparam>
    /// <param name="services">The services available in the application.</param>
    public static IServiceCollection TryAddPlatformEventHandler<TEvent, TEventHandler>(this IServiceCollection services)
        where TEvent : IPlatformEvent
        where TEventHandler : class, IPlatformEventHandler<TEvent> {
        services.TryAddTransient(typeof(IPlatformEventHandler<TEvent>), typeof(TEventHandler));
        return services;
    }
}
