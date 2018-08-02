using System;
using System.IO;
using System.Text.RegularExpressions;
using Indice.AspNetCore.TagHelpers;
using Indice.Configuration;
using Indice.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;

namespace Indice.AspNetCore.Extensions
{
    /// <summary>
    /// Extensions to configure the IServiceCollection of an ASP.NET Core application.
    /// </summary>
    public static class ServiceCollectionExtensions {
        /// <summary>
        /// Adds Indice common services.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddIndiceServices(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<GeneralSettings>(configuration.GetSection(GeneralSettings.Name));
            return services;
        }

        /// <summary>
        /// Adds EmailService that uses Sparkpost to send and Razor templates.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddEmailServiceSparkpost(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<EmailServiceSparkPostSettings>(configuration.GetSection(EmailServiceSparkPostSettings.Name));
            services.AddTransient((sp) => sp.GetRequiredService<IOptions<EmailServiceSparkPostSettings>>().Value);
            services.AddTransient<IEmailService, EmailServiceSparkpost>();

            return services;
        }

        /// <summary>
        /// Adds EmailService using SMTP settings in configuration.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddEmailService(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<EmailServiceSettings>(configuration.GetSection(EmailServiceSettings.Name));
            services.AddTransient((sp) => sp.GetRequiredService<IOptions<EmailServiceSettings>>().Value);
            services.AddTransient<IEmailService, EmailServiceSmtp>();

            return services;
        }

        /// <summary>
        /// Adds SmsService using Youboto.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddSmsServiceYouboto(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<SmsServiceSettings>(configuration.GetSection(SmsServiceSettings.Name));
            services.AddTransient((sp) => sp.GetRequiredService<IOptions<SmsServiceSettings>>().Value);
            services.AddTransient<ISmsService, SmsServiceYuboto>();

            return services;
        }

        /// <summary>
        /// Adds <see cref="IFileService"/> using Azre blob storage as the backing store.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="action"></param>
        public static IServiceCollection AddFilesAzure(this IServiceCollection services, Action<FileServiceAzureStorage.FileServiceOptions> action = null) {
            services.AddTransient<IFileService, FileServiceAzureStorage>(sp => {
                var options = new FileServiceAzureStorage.FileServiceOptions {
                    ConnectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString(FileServiceAzureStorage.CONNECTION_STRING_NAME),
                    EnvironmentName = sp.GetRequiredService<IHostingEnvironment>().EnvironmentName
                };
                action?.Invoke(options);
                return new FileServiceAzureStorage(options.ConnectionString, options.EnvironmentName);
            });
            return services;
        }

        /// <summary>
        /// Adds <see cref="IFileService"/> using Local filesystem as the backing store.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="storageContainerPath"></param>
        public static IServiceCollection AddFilesLocal(this IServiceCollection services, string storageContainerPath = null) {
            services.AddTransient<IFileService, FileServiceLocal>(sp => {

                if (storageContainerPath == null) {
                    var hostingEnvironment = sp.GetRequiredService<IHostingEnvironment>();
                    var environmentName = Regex.Replace(hostingEnvironment.EnvironmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
                    storageContainerPath = Path.Combine(hostingEnvironment.ContentRootPath, "App_Data", environmentName);
                }
                if (!Directory.Exists(storageContainerPath)) {
                    if (!Directory.Exists(Path.GetDirectoryName(storageContainerPath))) {
                        Directory.CreateDirectory(Path.GetDirectoryName(storageContainerPath));
                    }
                    Directory.CreateDirectory(storageContainerPath);
                }
                return new FileServiceLocal(storageContainerPath);
            });
            return services;
        }

        /// <summary>
        /// Adds <see cref="IFileService"/> using in-memory storage as the backing store. Only for testing purposes.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddFilesInMemory(this IServiceCollection services) {
            services.AddTransient<IFileService, FileServiceInMemory>();
            return services;
        }

        /// <summary>
        /// Adds Markdig as a Markdown processor. Needed to use with ASP.NET <see cref="MdTagHelper"/>.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddMarkdown(this IServiceCollection services) {
            services.AddTransient<IMarkdownProcessor, MarkdigProcessor>();
            return services;
        }

        /// <summary>
        /// Configures the Data Protection API for the application by using Azure storage.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configure">Configure the available options. Null to use defaults</param>
        public static void AddDataProtectionAzure(this IServiceCollection services, Action<AzureDataProtectionOptions> configure = null) {
            services.TryAddSingleton(typeof(IDataProtectionEncryptor<>), typeof(DataProtectionEncryptor<>));
            var serviceProvider = services.BuildServiceProvider();

            var hostingEnvironment = serviceProvider.GetRequiredService<IHostingEnvironment>();
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
                    // This prevents the apps from understanding each other's protected payloads (e.x Azure slots). To share protected payloads between two apps, use SetApplicationName with the same value for each app.
                    .SetApplicationName(options.ApplicationName);
        }

        /// <summary>
        /// Configures the Data Protection API for the application by using the file system.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="path">The path to the file system that will be used within the data protection system.</param>
        /// <exception cref="ArgumentException">When the </exception>
        public static void AddDataProtectionLocal(this IServiceCollection services, string path = null) {
            var serviceProvider = services.BuildServiceProvider();
            var hostingEnvironment = serviceProvider.GetRequiredService<IHostingEnvironment>();
            if (path == null) {
                var environmentName = Regex.Replace(hostingEnvironment.EnvironmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
                path = Path.Combine(hostingEnvironment.ContentRootPath, "App_Data", environmentName);
            }

            if (!Directory.Exists(path)) {
                if (!Directory.Exists(Path.GetDirectoryName(path))) {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
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
                    // This prevents the apps from understanding each other's protected payloads (e.x Azure slots). To share protected payloads between two apps, use SetApplicationName with the same value for each app.
                    .SetApplicationName(hostingEnvironment.ApplicationName);
        }

        /// <summary>
        /// Options for configuring DataProtection API using azure Blob Storage backend.
        /// </summary>
        public class AzureDataProtectionOptions {
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
