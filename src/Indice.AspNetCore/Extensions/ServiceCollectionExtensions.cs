using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.TagHelpers;
using Indice.Configuration;
using Indice.Services;
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
    /// Extensions to configure the IServiceCollection of an ASP.NET Core application.
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
            services.AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptions<GeneralSettings>>().Value);
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
            services.AddHttpClient<ISmsService, SmsServiceYuboto>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
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
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static void AddDataProtectionAzure(this IServiceCollection services, Action<AzureDataProtectionOptions> configure = null) {
            services.TryAddSingleton(typeof(IDataProtectionEncryptor<>), typeof(DataProtectionEncryptor<>));
            var serviceProvider = services.BuildServiceProvider();
            var hostingEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            var environmentName = Regex.Replace(hostingEnvironment.EnvironmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
            var options = new AzureDataProtectionOptions {
                StorageConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("StorageConnection"),
                ContainerName = environmentName,
                ApplicationName = hostingEnvironment.ApplicationName
            };
            configure?.Invoke(options);
            var storageAccount = CloudStorageAccount.Parse(options.StorageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(options.ContainerName);
            container.CreateIfNotExistsAsync().Wait();
            // Enables data protection services to the specified IServiceCollection.
            services.AddDataProtection()
                    // Configures the data protection system to use the specified cryptographic algorithms by default when generating protected payloads.
                    // The algorithms selected below are the default and they are added just for completeness.
                    .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration {
                        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
                        ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
                    })
                    .PersistKeysToAzureBlobStorage(container, "Keys")
                    // Configure the system to use a key lifetime of 30 days instead of the default 90 days.
                    .SetDefaultKeyLifetime(TimeSpan.FromDays(30))
                    // Configure the system not to automatically roll keys (create new keys) as they approach expiration.
                    //.DisableAutomaticKeyGeneration()
                    // This prevents the apps from understanding each other's protected payloads (e.x Azure slots). To share protected payloads between two apps, use SetApplicationName with the 
                    // same value for each app.
                    .SetApplicationName(options.ApplicationName);
        }

        /// <summary>
        /// Configures the Data Protection API for the application by using the file system.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="path">The path to the file system that will be used within the data protection system.</param>
        public static void AddDataProtectionLocal(this IServiceCollection services, string path = null) {
            var serviceProvider = services.BuildServiceProvider();
            var hostingEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            if (path == null) {
                var environmentName = Regex.Replace(hostingEnvironment.EnvironmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
                path = Path.Combine(hostingEnvironment.ContentRootPath, "App_Data");
            }
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            services.TryAddSingleton(typeof(IDataProtectionEncryptor<>), typeof(DataProtectionEncryptor<>));
            // Enables data protection services to the specified IServiceCollection.
            services.AddDataProtection()
                    // Configures the data protection system to use the specified cryptographic algorithms by default when generating protected payloads.
                    // The algorithms selected below are the default and they are added just for completeness.
                    .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration {
                        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
                        ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
                    })
                    .PersistKeysToFileSystem(new DirectoryInfo(path))
                    // Configure the system to use a key lifetime of 30 days instead of the default 90 days.
                    .SetDefaultKeyLifetime(TimeSpan.FromDays(30))
                    // Configure the system not to automatically roll keys (create new keys) as they approach expiration.
                    //.DisableAutomaticKeyGeneration()
                    // This prevents the apps from understanding each other's protected payloads (e.x Azure slots). To share protected payloads between two apps, use SetApplicationName with the 
                    // same value for each app.
                    .SetApplicationName(hostingEnvironment.ApplicationName);
        }

        /// <summary>
        /// Options for configuring ASP.NET Core DataProtection API using azure Blob Storage backend.
        /// </summary>
        public class AzureDataProtectionOptions
        {
            /// <summary>
            /// The connection string to your Azure storage account.
            /// </summary>
            public string StorageConnectionString { get; set; }
            /// <summary>
            /// The name of the container that will be used within the data protection system.
            /// </summary>
            public string ContainerName { get; set; }
            /// <summary>
            /// Sets the unique name of this application within the data protection system.
            /// </summary>
            public string ApplicationName { get; set; }
        }
    }
}
