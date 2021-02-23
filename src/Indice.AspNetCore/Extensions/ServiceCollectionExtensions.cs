using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using Indice.AspNetCore.Configuration;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.TagHelpers;
using Indice.Configuration;
using Indice.Services;
using Indice.Services.Yuboto;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions to configure the <see cref="IServiceCollection"/> of an ASP.NET Core application.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds content security policy. See also <see cref="SecurityHeadersAttribute"/> that enables the policy on a specific action.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configureAction"></param>
        /// <returns></returns>
        public static IServiceCollection AddCsp(this IServiceCollection services, Action<CSP> configureAction = null) {
            var policy = CSP.DefaultPolicy.Clone();
            configureAction?.Invoke(policy);
            services.AddSingleton(policy);
            return services;
        }

        /// <summary>
        /// Adds Indice's common services.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddIndiceServices(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<GeneralSettings>(configuration.GetSection(GeneralSettings.Name));
            services.TryAddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<GeneralSettings>>().Value);
            return services;
        }

        /// <summary>
        /// Adds an instance of <see cref="IEmailService"/> that uses Sparkpost to send and Razor templates.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddEmailServiceSparkpost(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<EmailServiceSparkPostSettings>(configuration.GetSection(EmailServiceSparkPostSettings.Name));
            services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<EmailServiceSparkPostSettings>>().Value);
            services.AddHttpClient<IEmailService, EmailServiceSparkpost>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
            return services;
        }

        /// <summary>
        /// Adds an instance of <see cref="IEmailService"/> using SMTP settings in configuration plus Razor email templates.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddEmailServiceSmtpRazor(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<EmailServiceSettings>(configuration.GetSection(EmailServiceSettings.Name));
            services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<EmailServiceSettings>>().Value);
            services.AddTransient<IEmailService, EmailServiceSmtpRazor>();
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
        public static IServiceCollection AddSmsServiceYouboto(this IServiceCollection services, IConfiguration configuration) {
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
        public static IServiceCollection AddEventDispatcherAzure(this IServiceCollection services, Action<EventDispatcherOptions> configure = null) {
            services.AddTransient<IEventDispatcher, EventDispatcherAzure>(serviceProvider => {
                var options = new EventDispatcherOptions {
                    ConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(EventDispatcherAzure.CONNECTION_STRING_NAME),
                    Enabled = true,
                    EnvironmentName = serviceProvider.GetRequiredService<IWebHostEnvironment>().EnvironmentName
                };
                configure?.Invoke(options);
                return new EventDispatcherAzure(options.ConnectionString, options.EnvironmentName, options.Enabled, serviceProvider.GetRequiredService<IHttpContextAccessor>());
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

        /// <summary>
        /// Adds <see cref="IFileService"/> using Azure Blob Storage as the backing store.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static IServiceCollection AddFilesAzure(this IServiceCollection services, Action<FileServiceAzureStorage.FileServiceOptions> configure = null) {
            services.AddTransient<IFileService, FileServiceAzureStorage>(serviceProvider => {
                var options = new FileServiceAzureStorage.FileServiceOptions {
                    ConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(FileServiceAzureStorage.CONNECTION_STRING_NAME),
                    EnvironmentName = serviceProvider.GetRequiredService<IWebHostEnvironment>().EnvironmentName
                };
                configure?.Invoke(options);
                return new FileServiceAzureStorage(options.ConnectionString, options.EnvironmentName);
            });
            return services;
        }

        /// <summary>
        /// Adds <see cref="IFileService"/> using local filesystem as the backing store.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="path">The path to use save.</param>
        public static IServiceCollection AddFilesLocal(this IServiceCollection services, string path = null) {
            services.AddTransient<IFileService, FileServiceLocal>(serviceProvider => {
                if (path == null) {
                    var hostingEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                    var environmentName = Regex.Replace(hostingEnvironment.EnvironmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
                    path = Path.Combine(hostingEnvironment.ContentRootPath, "App_Data");
                }
                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }
                return new FileServiceLocal(path);
            });
            return services;
        }

        /// <summary>
        /// Adds <see cref="IFileService"/> using in-memory storage as the backing store. Only for testing purposes.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IServiceCollection AddFilesInMemory(this IServiceCollection services) {
            services.AddTransient<IFileService, FileServiceInMemory>();
            return services;
        }

        /// <summary>
        /// Adds Markdig as a Markdown processor. Needed to use with ASP.NET <see cref="MdTagHelper"/>.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IServiceCollection AddMarkdown(this IServiceCollection services) {
            services.AddTransient<IMarkdownProcessor, MarkdigProcessor>();
            return services;
        }

        /// <summary>
        /// Configures the Data Protection API for the application by using Azure Storage.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configure">Configures the available options. Null to use defaults.</param>
        public static IServiceCollection AddDataProtectionAzure(this IServiceCollection services, Action<AzureDataProtectionOptions> configure = null) {
            services.TryAddSingleton(typeof(IDataProtectionEncryptor<>), typeof(DataProtectionEncryptor<>));
            var serviceProvider = services.BuildServiceProvider();
            var hostingEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            var environmentName = Regex.Replace(hostingEnvironment.EnvironmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
            const int defaultKeyLifetime = 90;
            var options = new AzureDataProtectionOptions {
                StorageConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("StorageConnection"),
                ContainerName = environmentName,
                ApplicationName = hostingEnvironment.ApplicationName,
                KeyLifetime = defaultKeyLifetime
            };
            options.Services = services;
            configure?.Invoke(options);
            options.Services = null;
            if (options.KeyLifetime <= 0) {
                options.KeyLifetime = defaultKeyLifetime;
            }
            var storageAccount = CloudStorageAccount.Parse(options.StorageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(options.ContainerName);
            container.CreateIfNotExistsAsync().Wait();
            // Enables data protection services to the specified IServiceCollection.
            var dataProtectionBuilder = services.AddDataProtection()
                                                // Configures the data protection system to use the specified cryptographic algorithms by default when generating protected payloads.
                                                // The algorithms selected below are the default and they are added just for completeness.
                                                .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration {
                                                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
                                                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
                                                })
                                                .PersistKeysToAzureBlobStorage(container, "keys.xml")
                                                // Configure the system to use a key lifetime. Default is 90 days.
                                                .SetDefaultKeyLifetime(TimeSpan.FromDays(options.KeyLifetime))
                                                // This prevents the apps from understanding each other's protected payloads (e.x Azure slots). To share protected payloads between two apps, 
                                                // use SetApplicationName with the same value for each app.
                                                .SetApplicationName(options.ApplicationName);
            if (options.DisableAutomaticKeyGeneration) {
                // Configure the system not to automatically roll keys (create new keys) as they approach expiration.
                dataProtectionBuilder.DisableAutomaticKeyGeneration();
            }
            return services;
        }

        /// <summary>
        /// Configures the Data Protection API for the application by using the file system.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configure">Configures the available options. Null to use defaults.</param>
        public static IServiceCollection AddDataProtectionLocal(this IServiceCollection services, Action<LocalDataProtectionOptions> configure = null) {
            var serviceProvider = services.BuildServiceProvider();
            var hostingEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            const int defaultKeyLifetime = 90;
            var options = new LocalDataProtectionOptions {
                ApplicationName = hostingEnvironment.ApplicationName,
                CryptographicAlgorithms = new AuthenticatedEncryptorConfiguration {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
                },
                KeyLifetime = defaultKeyLifetime
            };
            options.Services = services;
            configure?.Invoke(options);
            options.Services = null;
            if (options.KeyLifetime <= 0) {
                options.KeyLifetime = defaultKeyLifetime;
            }
            if (string.IsNullOrWhiteSpace(options.Path)) {
                options.Path = Path.Combine(hostingEnvironment.ContentRootPath, "App_Data");
            } else if (!Path.IsPathRooted(options.Path)) {
                options.Path = Path.Combine(hostingEnvironment.ContentRootPath, options.Path);
            }
            if (!Directory.Exists(options.Path)) {
                Directory.CreateDirectory(options.Path);
            }
            services.TryAddSingleton(typeof(IDataProtectionEncryptor<>), typeof(DataProtectionEncryptor<>));
            // Enables data protection services to the specified IServiceCollection.
            var dataProtectionBuilder = services.AddDataProtection()
                                                // Configures the data protection system to use the specified cryptographic algorithms by default when generating protected payloads.
                                                // The algorithms selected below are the default and they are added just for completeness.
                                                .UseCryptographicAlgorithms(options.CryptographicAlgorithms)
                                                .PersistKeysToFileSystem(new DirectoryInfo(options.Path))
                                                // Configure the system to use a key lifetime. Default is 90 days.
                                                .SetDefaultKeyLifetime(TimeSpan.FromDays(options.KeyLifetime))
                                                // This prevents the apps from understanding each other's protected payloads (e.x Azure slots). To share protected payloads between two apps, 
                                                // use SetApplicationName with the same value for each app.
                                                .SetApplicationName(options.ApplicationName);
            if (options.DisableAutomaticKeyGeneration) {
                // Configure the system not to automatically roll keys (create new keys) as they approach expiration.
                dataProtectionBuilder.DisableAutomaticKeyGeneration();
            }
            return services;
        }

        /// <summary>
        /// Reads the data protection options directly from configuration.
        /// </summary>
        /// <param name="options">Options for configuring ASP.NET Core DataProtection API using local file system.</param>
        /// <param name="section">The section to use in search for settings regarding data protection. Default section used is <see cref="LocalDataProtectionOptions.Name"/>.</param>
        public static LocalDataProtectionOptions FromConfiguration(this LocalDataProtectionOptions options, string section = null) {
            var serviceProvider = options.Services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            configuration.Bind(section ?? LocalDataProtectionOptions.Name, options);
            return options;
        }

        /// <summary>
        /// Reads the data protection options directly from configuration.
        /// </summary>
        /// <param name="options">Options for configuring ASP.NET Core DataProtection API using Azure Blob Storage infrastructure.</param>
        /// <param name="section">The section to use in search for settings regarding data protection. Default section used is <see cref="LocalDataProtectionOptions.Name"/>.</param>
        public static AzureDataProtectionOptions FromConfiguration(this AzureDataProtectionOptions options, string section = null) {
            var serviceProvider = options.Services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            configuration.Bind(section ?? AzureDataProtectionOptions.Name, options);
            return options;
        }
    }
}
