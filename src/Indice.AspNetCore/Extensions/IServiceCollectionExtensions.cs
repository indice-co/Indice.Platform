using System.Data;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Indice.AspNetCore.Configuration;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Middleware;
using Indice.AspNetCore.TagHelpers;
using Indice.Configuration;
using Indice.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions to configure the <see cref="IServiceCollection"/> of an ASP.NET Core application.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures the content security policy (<strong>Content-Security-Policy</strong> header).<br />
    /// Configures the use of <see cref="SecurityHeadersMiddleware"/> as well as the <seealso cref="SecurityHeadersAttribute"/> (MVC filter that enables the policy on a specific action).
    /// </summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configureAction">Configuration action.</param>
    /// <remarks>Better use the more complete version <see cref="AddSecurityHeaders(IServiceCollection, Action{SecurityHeadersPolicy})"/></remarks>
    public static IServiceCollection AddCsp(this IServiceCollection services, Action<CSP> configureAction = null) {
        var cspPolicy = CSP.DefaultPolicy.Clone();
        configureAction?.Invoke(cspPolicy);
        services.AddSecurityHeaders(policy => {
            policy.ContentSecurityPolicy = cspPolicy;
        });
        return services;
    }

    /// <summary>
    /// Configures the following header options: <strong>Content-Security-Policy</strong>, <strong>X-Frame-Options</strong>, <strong>Referrer-Policy</strong>, <strong>X-Content-Type-Options</strong>. <br />
    /// Configures the use of <see cref="SecurityHeadersMiddleware"/> as well as the <seealso cref="SecurityHeadersAttribute"/> (MVC filter that enables the policy on a specific action).
    /// </summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configureAction">Configuration action.</param>
    public static IServiceCollection AddSecurityHeaders(this IServiceCollection services, Action<SecurityHeadersPolicy> configureAction = null) {
        var policy = new SecurityHeadersPolicy();
        configureAction?.Invoke(policy);
        services.TryAddSingleton(policy);
        return services;
    }

    /// <summary>Configures content security policy (<strong>Content-Security-Policy</strong> header) options <see cref="CSP"/> from <seealso cref="IConfiguration"/>.<br /></summary>
    /// <param name="policy">the CSP policy to configure.</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="sectionName">Configuration section name.</param>
    /// <remarks>Better use the more complete version <see cref="AddSecurityHeaders(IServiceCollection, Action{SecurityHeadersPolicy})"/>.</remarks>
    public static CSP FromConfiguration(this CSP policy, IConfiguration configuration, string sectionName = null) {
        configuration.Bind(nameof(CSP), policy);
        return policy;
    }

    /// <summary>Adds an instance of <see cref="IHtmlRenderingEngine"/> for generating HTML content for use cases like email sending and other non HTTP related operations.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddHtmlRenderingEngineRazorMvc(this IServiceCollection services) {
        services.AddTransient<IHtmlRenderingEngine, HtmlRenderingEngineMvcRazor>();
        return services;
    }

    /// <summary>Registers <see cref="HtmlRenderingEngineMvcRazor"/> to be used by the <see cref="IEmailService"/> implementation.</summary>
    /// <param name="builder">Builder class for <see cref="IEmailService"/>.</param>
    public static IServiceCollection WithMvcRazorRendering(this EmailServiceBuilder builder) {
        builder.WithHtmlRenderingEngine<HtmlRenderingEngineMvcRazor>();
        return builder.Services;
    }

    /// <summary>Adds Markdig as a Markdown processor. Needed to use with ASP.NET <see cref="MdTagHelper"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddMarkdown(this IServiceCollection services) {
        services.AddTransient<IMarkdownProcessor, MarkdigProcessor>();
        return services;
    }

    /// <summary>Configures the Data Protection API for the application by using Azure Storage.</summary>
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
        var container = new BlobContainerClient(options.StorageConnectionString, options.ContainerName);
        container.CreateIfNotExists();
        // Enables data protection services to the specified IServiceCollection.
        var dataProtectionBuilder = services.AddDataProtection()
                                            // Configures the data protection system to use the specified cryptographic algorithms by default when generating protected payloads.
                                            // The algorithms selected below are the default and they are added just for completeness.
                                            .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration {
                                                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
                                                ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
                                            })
                                            .PersistKeysToAzureBlobStorage(options.StorageConnectionString, options.ContainerName, "keys.xml")
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

    /// <summary>Configures the Data Protection API for the application by using the file system.</summary>
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

    /// <summary>Reads the data protection options directly from configuration.</summary>
    /// <param name="options">Options for configuring ASP.NET Core DataProtection API using local file system.</param>
    /// <param name="section">The section to use in search for settings regarding data protection. Default section used is <see cref="LocalDataProtectionOptions.Name"/>.</param>
    public static LocalDataProtectionOptions FromConfiguration(this LocalDataProtectionOptions options, string section = null) {
        var serviceProvider = options.Services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        configuration.Bind(section ?? LocalDataProtectionOptions.Name, options);
        return options;
    }

    /// <summary>Reads the data protection options directly from configuration.</summary>
    /// <param name="options">Options for configuring ASP.NET Core DataProtection API using Azure Blob Storage infrastructure.</param>
    /// <param name="section">The section to use in search for settings regarding data protection. Default section used is <see cref="LocalDataProtectionOptions.Name"/>.</param>
    public static AzureDataProtectionOptions FromConfiguration(this AzureDataProtectionOptions options, string section = null) {
        var serviceProvider = options.Services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        configuration.Bind(section ?? AzureDataProtectionOptions.Name, options);
        return options;
    }

    /// <summary>Removes the specified type implementation for <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <exception cref="ReadOnlyException"></exception>
    public static IServiceCollection Remove<T>(this IServiceCollection services) where T : class {
        if (services.IsReadOnly) {
            throw new ReadOnlyException($"{nameof(services)} is read only.");
        }
        var serviceDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(T));
        if (serviceDescriptor is not null) {
            services.Remove(serviceDescriptor);
        }
        return services;
    }

    /// <summary>
    /// Configures the limit upload options for <see cref="AllowedFileExtensionsAttribute"/> and <see cref="AllowedFileSizeAttribute"/>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureAction">The action to configure. Use null for default options.</param>
    /// <returns></returns>
    public static IServiceCollection AddLimitUpload(this IServiceCollection services, Action<LimitUploadOptions> configureAction = null) {
        var options = new LimitUploadOptions();
        configureAction?.Invoke(options);
        
        services.Configure<LimitUploadOptions>(configureAction ?? (options => {
            options.DefaultMaxFileSizeBytes = options.DefaultMaxFileSizeBytes;
            options.DefaultAllowedFileExtensions = options.DefaultAllowedFileExtensions;
        }));

        return services;
    }

    /// <summary>
    /// Configures the limit upload options for <see cref="AllowedFileExtensionsAttribute"/> and <see cref="AllowedFileSizeAttribute"/>
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="configuration">The IConfiguration.</param>
    /// <returns></returns>
    public static IServiceCollection AddLimitUpload(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<LimitUploadOptions>(configuration);
        return services;
    }
}
