using System.Net.Mime;
using System.Security.Claims;
using FluentValidation;
using FluentValidation.AspNetCore;
using Indice.AspNetCore.Swagger;
using Indice.Events;
using Indice.Features.Messages.AspNetCore.Authorization;
using Indice.Features.Messages.AspNetCore.Services;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Manager;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;
using Indice.Serialization;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Contains extension methods on <see cref="IServiceCollection"/> for configuring Messaging API endpoints.</summary>
public static class MessageFeatureExtensions
{
    /// <summary>Adds all Messages (both management and self-service) dependencies to the DI.</summary>
    /// <param name="services">The service collection.</param> 
    /// <param name="configureAction">Configuration for several options of Campaigns API feature.</param>
    public static IServiceCollection AddMessaging(this IServiceCollection services, Action<MessageEndpointOptions> configureAction = null) {
        // Configure options.
        var apiOptions = new MessageEndpointOptions(services);
        configureAction?.Invoke(apiOptions);

        return services.AddMessageManagement(options => {
            options.ApiPrefix = apiOptions.ApiPrefix;
            options.ConfigureDbContext = apiOptions.ConfigureDbContext;
            options.DatabaseSchema = apiOptions.DatabaseSchema;
            options.RequiredScope = apiOptions.RequiredScope;
            options.UserClaimType = apiOptions.UserClaimType;
            options.GroupName = apiOptions.ManagementGroupName;
            options.FileUploadLimit = apiOptions.FileUploadLimit;
        })
        .AddMessageInbox(options => {
            options.ApiPrefix = apiOptions.ApiPrefix;
            options.ConfigureDbContext = apiOptions.ConfigureDbContext;
            options.DatabaseSchema = apiOptions.DatabaseSchema;
            options.UserClaimType = apiOptions.UserClaimType;
            options.GroupName = apiOptions.InboxGroupName;
        });
    }

    /// <summary>Adds Messages management dependencies to the DI.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureAction">Configuration for several options of Campaigns management API feature.</param>
    public static IServiceCollection AddMessageManagement(this IServiceCollection services, Action<MessageManagementOptions> configureAction = null) {
        // Configure options.
        var apiOptions = new MessageManagementOptions(services);
        configureAction?.Invoke(apiOptions);

        // Configure authorization. It's important to register the authorization policy provider at this point.
        services.AddAuthorization(policy => policy.AddCampaignsManagementPolicy(apiOptions.RequiredScope))
                .AddTransient<IAuthorizationHandler, BeCampaignManagerHandler>();

        services.AddCampaignCore(apiOptions);

        services.Configure<MessageManagementOptions>(options => {
            options.ApiPrefix = apiOptions.ApiPrefix;
            options.ConfigureDbContext = apiOptions.ConfigureDbContext;
            options.DatabaseSchema = apiOptions.DatabaseSchema;
            options.RequiredScope = apiOptions.RequiredScope;
            options.UserClaimType = apiOptions.UserClaimType;
            options.RequiredScope = apiOptions.RequiredScope;
            options.GroupName = apiOptions.GroupName;
            options.FileUploadLimit = apiOptions.FileUploadLimit;
        });
        services.AddSingleton(new DatabaseSchemaNameResolver(apiOptions.DatabaseSchema));
        // Register framework services.
        services.AddHttpContextAccessor();
        // Register events.
        services.TryAddTransient<IPlatformEventService, DefaultPlatformEventService>();
        services.TryAddTransient<IContactService, ContactService>();
        services.TryAddTransient<ITemplateService, TemplateService>();
        services.TryAddTransient<ICampaignAttachmentService, CampaignAttachmentService>();
        services.TryAddTransient<NotificationsManager>();
        services.TryAddTransient<IDistributionListService, DistributionListService>();
        services.TryAddTransient<ICampaignService, CampaignService>();
        services.TryAddTransient<IMessageTypeService, MessageTypeService>();
        services.TryAddTransient<IMessageSenderService, MessageSenderService>();
        services.TryAddTransient<CreateCampaignRequestValidator>();
        services.TryAddTransient<CreateMessageTypeRequestValidator>();
        return services;
    }

    /// <summary>Adds Messages inbox API dependencies.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureAction">Configuration for several options of Campaigns inbox API feature.</param>
    public static IServiceCollection AddMessageInbox(this IServiceCollection services, Action<MessageInboxOptions> configureAction = null) {
        // Configure options.
        var apiOptions = new MessageInboxOptions(services);
        configureAction?.Invoke(apiOptions);

        services.AddCampaignCore(apiOptions);

        services.Configure<MessageInboxOptions>(options => {
            options.ApiPrefix = apiOptions.ApiPrefix;
            options.ConfigureDbContext = apiOptions.ConfigureDbContext;
            options.DatabaseSchema = apiOptions.DatabaseSchema;
            options.UserClaimType = apiOptions.UserClaimType;
            options.GroupName = apiOptions.GroupName;
        });
        services.AddSingleton(new DatabaseSchemaNameResolver(apiOptions.DatabaseSchema));
        return services;
    }

    /// <summary>
    /// Registers all the basic dependencies for the campaign tool with sensible defaults.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseOptions">The base campaign tool basic options</param>
    /// <returns>The service collection</returns>
    internal static IServiceCollection AddCampaignCore(this IServiceCollection services, CampaignOptionsBase baseOptions) {
        // Post configure JSON options.
        services.PostConfigure<JsonOptions>(options => {
            var enumFlagsConverterExists = options.JsonSerializerOptions.Converters.Any(converter => converter.GetType() == typeof(JsonStringArrayEnumFlagsConverterFactory));
            if (!enumFlagsConverterExists) {
                options.JsonSerializerOptions.Converters.Insert(0, new JsonStringArrayEnumFlagsConverterFactory());
            }
        });
        // Post configure Swagger options.
        services.PostConfigure<SwaggerGenOptions>(options => {
            var enumFlagsSchemaFilterExists = options.SchemaFilterDescriptors.Any(x => x.Type == typeof(EnumFlagsSchemaFilter));
            if (!enumFlagsSchemaFilterExists) {
                options.SchemaFilter<EnumFlagsSchemaFilter>();
            }
        });
        // Register validators.
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<CreateCampaignRequestValidator>();
        // Register framework services.
        services.AddResponseCaching();
        // Register custom services.
        services.TryAddTransient<ICampaignService, CampaignService>();
        services.TryAddTransient<IMessageTypeService, MessageTypeService>();
        services.TryAddTransient<IMessageSenderService, MessageSenderService>();
        services.TryAddTransient<IDistributionListService, DistributionListService>();
        services.TryAddTransient<IMessageService, MessageService>();
        services.TryAddScoped<IUserNameAccessor, UserNameFromClaimsAccessor>();
        services.TryAddScoped<UserNameAccessorAggregate>();
        services.TryAddTransient<IFileService, FileServiceNoop>();
        services.TryAddTransient<IFileServiceFactory, DefaultFileServiceFactory>();
        services.TryAddTransient<IContactResolver, ContactResolverNoop>();
        services.AddEventDispatcherNoop();
        services.AddFilesNoop();
        // Register application DbContext.
        Action<IServiceProvider, DbContextOptionsBuilder> sqlServerConfiguration = (serviceProvider, builder) => builder.UseSqlServer(serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("MessagesDbConnection"));
        services.AddDbContext<CampaignsDbContext>(baseOptions.ConfigureDbContext ?? sqlServerConfiguration);
        return services;
    }

    /// <summary>Adds <see cref="IFileService"/> using local file system as the backing store.</summary>
    /// <param name="options">Options used to configure the Campaigns API feature.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static void UseFilesLocal(this CampaignOptionsBase options, Action<FileServiceLocalOptions> configure = null) =>
        options.Services.AddFiles(options => options.AddFileSystem(KeyedServiceNames.FileServiceKey, configure));

    /// <summary>Adds <see cref="IFileService"/> using Azure Blob Storage as the backing store.</summary>
    /// <param name="options">Options used to configure the Campaigns API feature.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static void UseFilesAzure(this CampaignOptionsBase options, Action<FileServiceAzureOptions> configure = null) =>
        options.Services.AddFiles(options => options.AddAzureStorage(KeyedServiceNames.FileServiceKey, configure));

    /// <summary>Configures that campaign contact information will be resolved by contacting the Identity Server instance.</summary>
    /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
    /// <param name="configure">Delegate used to configure <see cref="ContactResolverIdentity"/> service.</param>
    public static MessageEndpointOptions UseIdentityContactResolver(this MessageEndpointOptions options, Action<ContactResolverIdentityOptions> configure) {
        UseIdentityContactResolverInternal(options, configure);
        return options;
    }

    /// <summary>Adds a custom contact resolver that discovers contact information from a third-party system.</summary>
    /// <typeparam name="TContactResolver">The concrete type of <see cref="IContactResolver"/>.</typeparam>
    /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
    public static MessageEndpointOptions UseContactResolver<TContactResolver>(this MessageEndpointOptions options) where TContactResolver : IContactResolver {
        UseContactResolverInternal<TContactResolver>(options);
        return options;
    }

    /// <summary>Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.</summary>
    /// <param name="options">Options used to configure the Campaigns API feature.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static void UseEventDispatcherAzure(this MessageEndpointOptions options, Action<IServiceProvider, MessageEventDispatcherAzureOptions> configure = null) {
        options.Services.AddEventDispatcherAzure(KeyedServiceNames.EventDispatcherServiceKey, (serviceProvider, options) => {
            var eventDispatcherOptions = new MessageEventDispatcherAzureOptions {
                ConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(EventDispatcherAzure.CONNECTION_STRING_NAME),
                Enabled = true,
                EnvironmentName = serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName,
                ClaimsPrincipalSelector = ClaimsPrincipal.ClaimsPrincipalSelector ?? (() => ClaimsPrincipal.Current)
            };
            configure?.Invoke(serviceProvider, eventDispatcherOptions);
            options.ClaimsPrincipalSelector = eventDispatcherOptions.ClaimsPrincipalSelector;
            options.ConnectionString = eventDispatcherOptions.ConnectionString;
            options.Enabled = eventDispatcherOptions.Enabled;
            options.EnvironmentName = eventDispatcherOptions.EnvironmentName;
            options.QueueMessageEncoding = eventDispatcherOptions.QueueMessageEncoding;
            options.TenantIdSelector = eventDispatcherOptions.TenantIdSelector;
            options.UseCompression = true;
        });
    }

#if NET7_0_OR_GREATER
#nullable enable
    /// <summary>Adds the Media Library feature.</summary>
    /// <param name="options">Options used to configure the Media API feature.</param>
    /// <param name="configureAction">Configure the available options. Null to use defaults.</param>
    public static void UseMediaLibrary(this CampaignOptionsBase options, Action<Indice.Features.Media.AspNetCore.MediaApiOptions>? configureAction = null) {
        options.Services.AddMediaLibrary(configureAction);
    }
#nullable disable
#endif
    /// <summary>Configures that campaign contact information will be resolved by contacting the Identity Server instance.</summary>
    /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
    /// <param name="configure">Delegate used to configure <see cref="ContactResolverIdentity"/> service.</param>
    public static MessageManagementOptions UseIdentityContactResolver(this MessageManagementOptions options, Action<ContactResolverIdentityOptions> configure) {
        UseIdentityContactResolverInternal(options, configure);
        return options;
    }

    /// <summary>Adds a custom contact resolver that discovers contact information from a third-party system.</summary>
    /// <typeparam name="TContactResolver">The concrete type of <see cref="IContactResolver"/>.</typeparam>
    /// <param name="options">Options for configuring internal campaign jobs used by the worker host.</param>
    public static MessageManagementOptions UseContactResolver<TContactResolver>(this MessageManagementOptions options) where TContactResolver : IContactResolver {
        UseContactResolverInternal<TContactResolver>(options);
        return options;
    }

    /// <summary>Adds multi-tenancy capabilities in the Messages API endpoints.</summary>
    /// <param name="options">Options used to configure the Messages API feature.</param>
    /// <param name="accessLevel">The minimum access level required.</param>
    public static MessageEndpointOptions UseMultiTenancy(this MessageEndpointOptions options, int accessLevel) {
        // Configure authorization. It's important to register the authorization policy provider at this point.
        options.Services.AddAuthorization(policy => policy.AddMultitenantCampaignsManagementPolicy(accessLevel, options.RequiredScope));
        options.Services.Configure<MessageMultitenancyOptions>(options => options.AccessLevel = accessLevel);
        return options;
    }

    /// <summary>Adds multi-tenancy capabilities in the Messages API endpoints.</summary>
    /// <param name="options">Options used to configure the Messages management API feature.</param>
    /// <param name="accessLevel">The minimum access level required.</param>
    public static MessageManagementOptions UseMultiTenancy(this MessageManagementOptions options, int accessLevel) {
        // Configure authorization. It's important to register the authorization policy provider at this point.
        options.Services.AddAuthorization(policy => policy.AddMultitenantCampaignsManagementPolicy(accessLevel, options.RequiredScope));
        options.Services.Configure<MessageMultitenancyOptions>(options => options.AccessLevel = accessLevel);
        return options;
    }
    private static void UseIdentityContactResolverInternal(CampaignOptionsBase options, Action<ContactResolverIdentityOptions> configure) {
        var serviceOptions = new ContactResolverIdentityOptions();
        serviceOptions.UserClaimType = options.UserClaimType;
        configure.Invoke(serviceOptions);
        options.Services.Configure<ContactResolverIdentityOptions>(config => {
            config.BaseAddress = serviceOptions.BaseAddress;
            config.ClientId = serviceOptions.ClientId;
            config.ClientSecret = serviceOptions.ClientSecret;
            config.UserClaimType = serviceOptions.UserClaimType;
        });
        options.Services.AddDistributedMemoryCache();
        options.Services.AddHttpClient<IContactResolver, ContactResolverIdentity>(httpClient => {
            httpClient.BaseAddress = serviceOptions.BaseAddress;
        });
    }

    private static void UseContactResolverInternal<TContactResolver>(CampaignOptionsBase options) where TContactResolver : IContactResolver =>
        options.Services.AddTransient(typeof(IContactResolver), typeof(TContactResolver));
}
