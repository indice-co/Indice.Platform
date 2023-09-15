using FluentValidation;
using Indice.Features.Media.AspNetCore;
using Indice.Features.Media.AspNetCore.Models.Requests;
using Indice.Features.Media.AspNetCore.Models.Validators;
using Indice.Features.Media.AspNetCore.Services;
using Indice.Features.Media.AspNetCore.Services.Hosting;
using Indice.Features.Media.AspNetCore.Stores;
using Indice.Features.Media.AspNetCore.Stores.Abstractions;
using Indice.Features.Media.Data;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Contains extension methods on <see cref="IServiceCollection"/> for configuring Media Management API feature.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds Media Management API endpoints in the project.</summary>
    /// <param name="services">An interface for configuring services.</param>
    /// <param name="configureAction">Configuration for several options of Media API feature.</param>
    public static IServiceCollection AddMediaLibrary(this IServiceCollection services, Action<MediaApiOptions>? configureAction = null) {
        // Register Default Policy Provider.
        //services.AddSingleton<IAuthorizationPolicyProvider, MediaLibraryPolicyProvider>();
        // Configure options.
        var apiOptions = new MediaApiOptions(services);
        configureAction?.Invoke(apiOptions);
        services.Configure<MediaApiOptions>(options => {
            options.ApiPrefix = apiOptions.ApiPrefix;
            options.ConfigureDbContext = apiOptions.ConfigureDbContext;
            options.AuthenticationScheme = apiOptions.AuthenticationScheme;
            options.AcceptableFileExtensions = apiOptions.AcceptableFileExtensions;
            options.ApiScope = apiOptions.ApiScope;
            options.MaxFileSize = apiOptions.MaxFileSize;
            options.UseSoftDelete = apiOptions.UseSoftDelete;
        });
        // Register framework services.
        services.AddHttpContextAccessor();
        // Register services.
        services.TryAddTransient<IMediaFolderStore, MediaFolderStore>();
        services.TryAddTransient<IMediaFileStore, MediaFileStore>();
        services.TryAddTransient<IUserNameAccessor, UserNameFromHttpContextAccessor>();
        services.TryAddTransient<MediaManager>();
        services.TryAddSingleton<Func<string, IFileService>>(serviceProvider => serviceKey => new FileServiceNoop());
        // Register validators
        services.AddEndpointParameterFluentValidation(typeof(MediaLibraryApi).Assembly);
        if(!apiOptions.UseSoftDelete) {
            services.AddHostedService<FoldersCleanUpHostedService>();
            services.AddHostedService<FilesCleanUpHostedService>();
        }
        // Register application DbContext.
        services.AddDbContext<MediaDbContext>(apiOptions.ConfigureDbContext ?? ((serviceProvider, builder) => builder.UseSqlServer(serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("MediaDbConnection"))));
        return services;
    }

    /// <summary>Adds <see cref="IAuthorizationPolicyProvider"/> using custom policy provider.</summary>
    /// <param name="options">Options used to configure the Media API feature.</param>
    public static void UsePolicyProvider<TAuthorizationPolicyProvider>(this MediaApiOptions options) where TAuthorizationPolicyProvider : IAuthorizationPolicyProvider =>
        options.Services?.AddSingleton(typeof(IAuthorizationPolicyProvider), typeof(TAuthorizationPolicyProvider));

    /// <summary>Adds <see cref="IFileService"/> using local file system as the backing store.</summary>
    /// <param name="options">Options used to configure the Media API feature.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static void UseFilesLocal(this MediaApiOptions options, Action<FileServiceLocalOptions>? configure = null) =>
        options.Services.AddFiles(options => options.AddFileSystem(KeyedServiceNames.FileServiceKey, configure));

    /// <summary>Adds <see cref="IFileService"/> using Azure Blob Storage as the backing store.</summary>
    /// <param name="options">Options used to configure the Media API feature.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static void UseFilesAzure(this MediaApiOptions options, Action<FileServiceAzureOptions>? configure = null) =>
        options.Services.AddFiles(options => options.AddAzureStorage(KeyedServiceNames.FileServiceKey, configure));
}
