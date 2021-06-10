using System;
using System.IO;
using System.Text.RegularExpressions;
using Indice.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="ServiceCollection"/> arround <see cref="IFileService"/>.
    /// </summary>
    public static class IServiceCollectionFilesExtensions
    {

        /// <summary>
        /// Adds <see cref="IFileService"/> using Azure Blob Storage as the backing store.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static IServiceCollection AddFilesAzure(this IServiceCollection services, Action<FileServiceAzureStorage.FileServiceOptions> configure = null) {
            services.AddTransient<IFileService, FileServiceAzureStorage>(serviceProvider => {
                var options = new FileServiceAzureStorage.FileServiceOptions {
                    ConnectionStringName = FileServiceAzureStorage.CONNECTION_STRING_NAME,
                    ContainerName = serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName
                };
                configure?.Invoke(options);
                var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(options.ConnectionStringName);
                return new FileServiceAzureStorage(connectionString, options.ContainerName);
            });
            return services;
        }

        /// <summary>
        /// Adds <see cref="IFileService"/> using local filesystem as the backing store.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static IServiceCollection AddFilesLocal(this IServiceCollection services, Action<FileServiceLocal.FileServiceOptions> configure = null) {
            services.AddTransient<IFileService, FileServiceLocal>(serviceProvider => {
                var options = new FileServiceLocal.FileServiceOptions() { Path = "App_Data" };
                configure?.Invoke(options);
                var hostingEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
                options.Path = Path.Combine(hostingEnvironment.ContentRootPath, options.Path);
                if (!Directory.Exists(options.Path)) {
                    Directory.CreateDirectory(options.Path);
                }
                return new FileServiceLocal(options.Path);
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
        /// Adds <see cref="IFileService"/> using in-memory storage as the backing store. Only for testing purposes.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configure">configuration action</param>
        public static IServiceCollection AddFiles(this IServiceCollection services, Action<FileServiceConfigurationBuilder> configure) {
            var builder = new FileServiceConfigurationBuilder(services);
            services.AddScoped<IFileServiceFactory, DefaultFileServiceFactory>();
            configure?.Invoke(builder);
            return services;
        }


        /// <summary>
        /// Adds <see cref="FileServiceInMemory"/> implementation
        /// </summary>
        /// <returns></returns>
        public static FileServiceConfigurationBuilder AddMemory(this FileServiceConfigurationBuilder builder) {
            AddFilesInMemory(builder.Services);
            return builder;
        }
        /// <summary>
        /// Adds <see cref="FileServiceInMemory"/> implementation
        /// </summary>
        /// <returns></returns>
        public static FileServiceConfigurationBuilder AddMemory(this FileServiceConfigurationBuilder builder, string name) {
            builder.Services.AddKeyedService<IFileService, FileServiceInMemory, string>(name, serviceLifetime: ServiceLifetime.Transient);
            return builder;
        }

        /// <summary>
        /// Adds <see cref="FileServiceLocal"/> implementation
        /// </summary>
        /// <returns></returns>
        public static FileServiceConfigurationBuilder AddFileSystem(this FileServiceConfigurationBuilder builder, Action<FileServiceLocal.FileServiceOptions> configure = null) {
            AddFilesLocal(builder.Services, configure);
            return builder;
        }

        /// <summary>
        /// Adds <see cref="FileServiceLocal"/> implementation
        /// </summary>
        /// <returns></returns>
        public static FileServiceConfigurationBuilder AddFileSystem(this FileServiceConfigurationBuilder builder, string name, Action<FileServiceLocal.FileServiceOptions> configure = null) {
            builder.Services.AddKeyedService<IFileService, FileServiceLocal, string>(
                key: name,
                serviceProvider => {
                    var options = new FileServiceLocal.FileServiceOptions() { Path = "App_Data" };
                    configure?.Invoke(options);
                    var hostingEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
                    options.Path = Path.Combine(hostingEnvironment.ContentRootPath, options.Path);
                    
                    if (!Directory.Exists(options.Path)) {
                        Directory.CreateDirectory(options.Path);
                    }
                    return new FileServiceLocal(options.Path);
                },
                serviceLifetime: ServiceLifetime.Transient);
            return builder;
        }
        /// <summary>
        /// Adds <see cref="FileServiceAzureStorage"/> implementation
        /// </summary>
        /// <returns></returns>
        public static FileServiceConfigurationBuilder AddAzureStorage(this FileServiceConfigurationBuilder builder, Action<FileServiceAzureStorage.FileServiceOptions> configure = null) {
            AddFilesAzure(builder.Services, configure);
            return builder;
        }

        /// <summary>
        /// Adds <see cref="FileServiceAzureStorage"/> implementation
        /// </summary>
        /// <returns></returns>
        public static FileServiceConfigurationBuilder AddAzureStorage(this FileServiceConfigurationBuilder builder, string name, Action<FileServiceAzureStorage.FileServiceOptions> configure = null) {
            builder.Services.AddKeyedService<IFileService, FileServiceAzureStorage, string>(
                key: name,
                serviceProvider => {
                    var options = new FileServiceAzureStorage.FileServiceOptions {
                        ConnectionStringName = FileServiceAzureStorage.CONNECTION_STRING_NAME,
                        ContainerName = serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName
                    };
                    configure?.Invoke(options);
                    var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(options.ConnectionStringName);
                    return new FileServiceAzureStorage(connectionString, options.ContainerName);
                },
                serviceLifetime: ServiceLifetime.Transient);
            return builder;
        }

        /// <summary>
        /// Confuguration builder for the app service
        /// </summary>
        public sealed class FileServiceConfigurationBuilder
        {
            /// <summary>
            /// constructs the builder
            /// </summary>
            /// <param name="services"></param>
            internal FileServiceConfigurationBuilder(IServiceCollection services) {
                Services = services;
            }

            /// <summary>
            /// Service collection
            /// </summary>
            public IServiceCollection Services { get; }
        }

        internal class DefaultFileServiceFactory : IFileServiceFactory
        {
            public DefaultFileServiceFactory(IServiceProvider serviceProvider) {
                _serviceProvider = serviceProvider;
            }
            private readonly IServiceProvider _serviceProvider;
            public IFileService Create(string name) => name is null ? _serviceProvider.GetRequiredService<IFileService>()
                                                                    : _serviceProvider.GetRequiredService<Func<string, IFileService>>()?.Invoke(name);
        }
    }
}
