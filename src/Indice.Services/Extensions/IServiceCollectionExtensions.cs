using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using Indice.Configuration;
using Indice.Services;
using Indice.Services.Yuboto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class IndiceServicesServiceCollectionExtensions
    {
        /// <summary>
        /// Add a decorator pattern implementation.
        /// </summary>
        /// <typeparam name="TService">The service type to decorate.</typeparam>
        /// <typeparam name="TDecorator">The decorator.</typeparam>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IServiceCollection AddDecorator<TService, TDecorator>(this IServiceCollection services)
            where TService : class
            where TDecorator : class, TService {
            var serviceDescriptor = services.Where(x => x.ServiceType == typeof(TService)).LastOrDefault();
            if (serviceDescriptor is null) {
                services.AddTransient<TService, TDecorator>();
                return services;
            }
            services.TryAddTransient(serviceDescriptor.ImplementationType);
            return services.AddTransient<TService, TDecorator>(serviceProvider => {
                var parameters = typeof(TDecorator).GetConstructors(BindingFlags.Public | BindingFlags.Instance).First().GetParameters();
                var arguments = parameters.Select(x => x.ParameterType.Equals(typeof(TService)) 
                    ? serviceProvider.GetRequiredService(serviceDescriptor.ImplementationType) 
                    : serviceProvider.GetService(x.ParameterType)).ToArray();
                return (TDecorator)Activator.CreateInstance(typeof(TDecorator), arguments);
            });
        }

        /// <summary>
        /// Adds Indice's common services.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddGeneralSettings(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<GeneralSettings>(configuration.GetSection(GeneralSettings.Name));
            services.TryAddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<GeneralSettings>>().Value);
            return services;
        }

        /// <summary>
        /// Adds an implementation of <see cref="IPushNotificationService"/> using Azure cloud infrastructure for sending push nitifications.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddPushNotificationServiceAzure(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<PushNotificationOptions>(configuration.GetSection(PushNotificationOptions.Name));
            services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<PushNotificationOptions>>().Value);
            services.AddTransient<IPushNotificationService, PushNotificationServiceAzure>();
            return services;
        }

        /// <summary>
        /// Adds an instance of <see cref="IEmailService"/> using SMTP settings in configuration.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddEmailService(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<EmailServiceSettings>(configuration.GetSection(EmailServiceSettings.Name));
            services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<EmailServiceSettings>>().Value);
            services.AddTransient<IEmailService, EmailServiceSmtp>();
            return services;
        }

        /// <summary>
        /// Adds an instance of <see cref="ISmsService"/> using Youboto.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddSmsServiceYuboto(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<SmsServiceSettings>(configuration.GetSection(SmsServiceSettings.Name));
            services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<SmsServiceSettings>>().Value);
            services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
            services.AddHttpClient<ISmsService, SmsServiceYuboto>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
            return services;
        }

        /// <summary>
        /// Adds an instance of <see cref="ISmsService"/> using Apifon.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static IServiceCollection AddSmsServiceApifon(this IServiceCollection services, IConfiguration configuration, Action<SmsServiceApifonOptions> configure = null) {
            services.Configure<SmsServiceApifonSettings>(configuration.GetSection(SmsServiceSettings.Name));
            services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<SmsServiceApifonSettings>>().Value);
            services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
            var options = new SmsServiceApifonOptions();
            configure?.Invoke(options);
            var httpClientBuilder = services.AddHttpClient<ISmsService, SmsServiceApifon>()
                                            .ConfigureHttpClient(httpClient => {
                                                httpClient.BaseAddress = new Uri("https://ars.apifon.com/services/api/v1/sms/");
                                            });
            if (options.ConfigurePrimaryHttpMessageHandler != null) {
                httpClientBuilder.ConfigurePrimaryHttpMessageHandler(options.ConfigurePrimaryHttpMessageHandler);
            }
            return services;
        }

        /// <summary>
        /// Adds an instance of <see cref="ISmsService"/> using Youboto.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddSmsServiceViber(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<SmsServiceViberSettings>(configuration.GetSection(SmsServiceViberSettings.Name));
            services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<SmsServiceViberSettings>>().Value);
            services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
            services.AddHttpClient<ISmsService, SmsServiceViber>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
            return services;
        }

        /// <summary>
        /// Adds an instance of <see cref="ISmsService"/> using Youboto Omni from sending regular SMS messages.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddSmsServiceYubotoOmni(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<SmsServiceSettings>(configuration.GetSection(SmsServiceSettings.Name));
            services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<SmsServiceSettings>>().Value);
            services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
            services.AddHttpClient<ISmsService, SmsYubotoOmniService>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
            return services;
        }

        /// <summary>
        /// Adds an instance of <see cref="ISmsService"/> using Youboto Omni for sending Viber messages.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddViberServiceYubotoOmni(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<SmsServiceSettings>(configuration.GetSection(SmsServiceSettings.Name));
            services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<SmsServiceSettings>>().Value);
            services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
            services.AddHttpClient<ISmsService, ViberYubotoOmniService>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
            return services;
        }

        /// <summary>
        /// Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static IServiceCollection AddEventDispatcherAzure(this IServiceCollection services, Action<IServiceProvider, EventDispatcherOptions> configure) {
            services.AddTransient<IEventDispatcher, EventDispatcherAzure>(serviceProvider => {
                var options = new EventDispatcherOptions {
                    ConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(EventDispatcherAzure.CONNECTION_STRING_NAME),
                    Enabled = true,
                    EnvironmentName = serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName,
                    ClaimsPrincipalSelector = ClaimsPrincipal.ClaimsPrincipalSelector ?? (() => ClaimsPrincipal.Current)
                };
                configure?.Invoke(serviceProvider, options);
                return new EventDispatcherAzure(options.ConnectionString, options.EnvironmentName, options.Enabled, options.MessageEncoding, options.ClaimsPrincipalSelector);
            });
            return services;
        }

        /// <summary>
        /// Adds <see cref="IEventDispatcher"/> using an in-memory <seealso cref="Queue"/> as a backing store.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IServiceCollection AddEventDispatcherInMemory(this IServiceCollection services) {
            services.AddTransient<IEventDispatcher, EventDispatcherInMemory>();
            return services;
        }

    }

}
