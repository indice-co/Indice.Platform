using Indice.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions on the <see cref="ServiceCollection"/> around <see cref="IFileService"/>.</summary>
public static class IServiceCollectionFileExtensions
{
    /// <summary>Adds <see cref="IFileService"/> using Azure Blob Storage as the backing store.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddFilesAzure(this IServiceCollection services, Action<FileServiceAzureOptions> configure = null) {
        services.AddTransient<IFileService, FileServiceAzureStorage>(serviceProvider => GetFileServiceAzureStorage(serviceProvider, configure));
        return services;
    }

    private static readonly Func<IServiceProvider, Action<FileServiceAzureOptions>, FileServiceAzureStorage> GetFileServiceAzureStorage = (serviceProvider, configure) => {
        var options = new FileServiceAzureOptions {
            ConnectionStringName = FileServiceAzureStorage.CONNECTION_STRING_NAME,
            ContainerName = serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName
        };
        configure?.Invoke(options);
        var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(options.ConnectionStringName);
        return new FileServiceAzureStorage(connectionString, options.ContainerName);
    };

    /// <summary>The factory that creates the default instance and configuration for <see cref="FileServiceLocal"/>.</summary>
    private static readonly Func<IServiceProvider, Action<FileServiceLocalOptions>, FileServiceLocal> GetFileServiceLocal = (serviceProvider, configure) => {
        var options = new FileServiceLocalOptions();
        configure?.Invoke(options);
        var hostingEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
        options.Path = Path.Combine(hostingEnvironment.ContentRootPath, options.Path);
        if (!Directory.Exists(options.Path)) {
            Directory.CreateDirectory(options.Path);
        }
        return new FileServiceLocal(options.Path);
    };

    /// <summary>Adds <see cref="IFileService"/> using local file system as the backing store.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddFilesLocal(this IServiceCollection services, Action<FileServiceLocalOptions> configure = null) {
        services.AddTransient<IFileService, FileServiceLocal>(serviceProvider => GetFileServiceLocal(serviceProvider, configure));
        return services;
    }

    /// <summary>Adds <see cref="IFileService"/> using in-memory storage as the backing store. Only for testing purposes.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddFilesInMemory(this IServiceCollection services) {
        services.AddTransient<IFileService, FileServiceInMemory>();
        return services;
    }

    /// <summary>Adds <see cref="IFileService"/> using in-memory storage as the backing store. Only for testing purposes.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configure">Configuration action.</param>
    public static IServiceCollection AddFiles(this IServiceCollection services, Action<FileServiceConfigurationBuilder> configure) {
        var builder = new FileServiceConfigurationBuilder(services);
        services.AddScoped<IFileServiceFactory, DefaultFileServiceFactory>();
        configure?.Invoke(builder);
        return services;
    }

    /// <summary>Adds <see cref="FileServiceInMemory"/> implementation.</summary>
    public static FileServiceConfigurationBuilder AddFilesInMemory(this FileServiceConfigurationBuilder builder) {
        AddFilesInMemory(builder.Services);
        return builder;
    }

    /// <summary>Adds <see cref="FileServiceInMemory"/> implementation.</summary>
    public static FileServiceConfigurationBuilder AddFilesInMemory(this FileServiceConfigurationBuilder builder, string name) {
        builder.Services.AddKeyedTransient<IFileService, FileServiceInMemory>(serviceKey: name);
        return builder;
    }

    /// <summary>Adds <see cref="FileServiceLocal"/> implementation.</summary>
    public static FileServiceConfigurationBuilder AddFileSystem(this FileServiceConfigurationBuilder builder, Action<FileServiceLocalOptions> configure = null) {
        AddFilesLocal(builder.Services, configure);
        return builder;
    }

    /// <summary>Adds <see cref="FileServiceLocal"/> implementation.</summary>
    public static FileServiceConfigurationBuilder AddFileSystem(this FileServiceConfigurationBuilder builder, string name, Action<FileServiceLocalOptions> configure = null) {
        builder.Services.AddKeyedTransient<IFileService, FileServiceLocal>(serviceKey: name, implementationFactory: (sp, serviceKey) => GetFileServiceLocal(sp, configure));
        return builder;
    }

    /// <summary>Adds <see cref="FileServiceAzureStorage"/> implementation.</summary>
    public static FileServiceConfigurationBuilder AddAzureStorage(this FileServiceConfigurationBuilder builder, Action<FileServiceAzureOptions> configure = null) {
        AddFilesAzure(builder.Services, configure);
        return builder;
    }

    /// <summary>Adds <see cref="FileServiceAzureStorage"/> implementation.</summary>
    public static FileServiceConfigurationBuilder AddAzureStorage(this FileServiceConfigurationBuilder builder, string name, Action<FileServiceAzureOptions> configure = null) {
        builder.Services.AddKeyedTransient<IFileService, FileServiceAzureStorage>(serviceKey: name, implementationFactory: (sp, serviceKey) => GetFileServiceAzureStorage(sp, configure));
        return builder;
    }

    /// <summary>Configuration builder for the app service.</summary>
    public sealed class FileServiceConfigurationBuilder
    {
        /// <summary>Constructs the builder.</summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        internal FileServiceConfigurationBuilder(IServiceCollection services) {
            Services = services;
        }

        /// <summary>Specifies the contract for a collection of service descriptors.</summary>
        public IServiceCollection Services { get; }
    }

    internal class DefaultFileServiceFactory : IFileServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultFileServiceFactory(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }

        public IFileService Create(string name) => name is null ? _serviceProvider.GetRequiredService<IFileService>() : _serviceProvider.GetRequiredService<Func<string, IFileService>>()?.Invoke(name);
    }
}
