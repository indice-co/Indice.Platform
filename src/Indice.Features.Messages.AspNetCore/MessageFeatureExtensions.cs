using System.Net.Mime;
using System.Security.Claims;
using FluentValidation;
using FluentValidation.AspNetCore;
using Indice.AspNetCore.Mvc.ApplicationModels;
using Indice.AspNetCore.Swagger;
using Indice.Events;
using Indice.Features.Messages.AspNetCore;
using Indice.Features.Messages.AspNetCore.Mvc.Authorization;
using Indice.Features.Messages.AspNetCore.Mvc.Formatters;
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

/// <summary>Contains extension methods on <see cref="IMvcBuilder"/> for configuring Campaigns API feature.</summary>
public static class MessageFeatureExtensions
{
    /// <summary>Adds all Messages (both management and self-service) API endpoints in the MVC project.</summary>
    /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
    /// <param name="configureAction">Configuration for several options of Campaigns API feature.</param>
    public static IMvcBuilder AddMessageEndpoints(this IMvcBuilder mvcBuilder, Action<MessageEndpointOptions> configureAction = null) {
        var services = mvcBuilder.Services;
        // Configure authorization. It's important to register the authorization policy provider at this point.
        services.AddSingleton<IAuthorizationPolicyProvider, CampaignsPolicyProvider>();
        // Configure options.
        var apiOptions = new MessageEndpointOptions(services);
        configureAction?.Invoke(apiOptions);
        return mvcBuilder.AddMessageManagementEndpoints(options => {
            options.ApiPrefix = apiOptions.ApiPrefix;
            options.ConfigureDbContext = apiOptions.ConfigureDbContext;
            options.DatabaseSchema = apiOptions.DatabaseSchema;
            options.RequiredScope = apiOptions.RequiredScope;
            options.UserClaimType = apiOptions.UserClaimType;
            options.GroupName = apiOptions.ManagementGroupName;
        })
        .AddMessageInboxEndpoints(options => {
            options.ApiPrefix = apiOptions.ApiPrefix;
            options.ConfigureDbContext = apiOptions.ConfigureDbContext;
            options.DatabaseSchema = apiOptions.DatabaseSchema;
            options.UserClaimType = apiOptions.UserClaimType;
            options.GroupName = apiOptions.InboxGroupName;
        });
    }

    /// <summary>Adds Messages management API endpoints in the MVC project.</summary>
    /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
    /// <param name="configureAction">Configuration for several options of Campaigns management API feature.</param>
    public static IMvcBuilder AddMessageManagementEndpoints(this IMvcBuilder mvcBuilder, Action<MessageManagementOptions> configureAction = null) {
        mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new MessageFeatureProvider(includeManagementApi: true, includeInboxApi: false)));
        var services = mvcBuilder.Services;
        // Configure options.
        var apiOptions = new MessageManagementOptions(services);
        configureAction?.Invoke(apiOptions);
        // Configure campaigns system core requirements.
        mvcBuilder.AddCampaignCore(apiOptions);
        services.Configure<MessageManagementOptions>(options => {
            options.ApiPrefix = apiOptions.ApiPrefix;
            options.ConfigureDbContext = apiOptions.ConfigureDbContext;
            options.DatabaseSchema = apiOptions.DatabaseSchema;
            options.RequiredScope = apiOptions.RequiredScope;
            options.UserClaimType = apiOptions.UserClaimType;
            options.RequiredScope = apiOptions.RequiredScope;
            options.GroupName = apiOptions.GroupName;
        });
        services.AddSingleton(new DatabaseSchemaNameResolver(apiOptions.DatabaseSchema));
        // Post configure MVC options.
        services.PostConfigure<MvcOptions>(options => {
            options.Conventions.Add(new ApiPrefixControllerModelConvention(ApiPrefixes.CampaignManagementEndpoints, apiOptions.ApiPrefix));
            options.Conventions.Add(new ApiGroupNameControllerModelConvention(ApiGroups.CampaignManagementEndpoints, apiOptions.GroupName));
            options.FormatterMappings.SetMediaTypeMappingForFormat("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            options.OutputFormatters.Add(new XlsxCampaignStatisticsOutputFormatter());
        });
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
        return mvcBuilder;
    }

    /// <summary>Adds Messages inbox API endpoints in the MVC project.</summary>
    /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
    /// <param name="configureAction">Configuration for several options of Campaigns inbox API feature.</param>
    public static IMvcBuilder AddMessageInboxEndpoints(this IMvcBuilder mvcBuilder, Action<MessageInboxOptions> configureAction = null) {
        mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new MessageFeatureProvider(includeManagementApi: false, includeInboxApi: true)));
        var services = mvcBuilder.Services;
        // Configure options.
        var apiOptions = new MessageInboxOptions(services);
        configureAction?.Invoke(apiOptions);
        mvcBuilder.AddCampaignCore(apiOptions);
        services.Configure<MessageInboxOptions>(options => {
            options.ApiPrefix = apiOptions.ApiPrefix;
            options.ConfigureDbContext = apiOptions.ConfigureDbContext;
            options.DatabaseSchema = apiOptions.DatabaseSchema;
            options.UserClaimType = apiOptions.UserClaimType;
            options.GroupName = apiOptions.GroupName;
        });
        services.AddSingleton(new DatabaseSchemaNameResolver(apiOptions.DatabaseSchema));
        // Post configure MVC options.
        services.PostConfigure<MvcOptions>(options => {
            options.Conventions.Add(new ApiPrefixControllerModelConvention(ApiPrefixes.MessageInboxEndpoints, apiOptions.ApiPrefix));
            options.Conventions.Add(new ApiGroupNameControllerModelConvention(ApiGroups.MessageInboxEndpoints, apiOptions.GroupName));
        });
        // Register custom services.
        return mvcBuilder;
    }

    private static IMvcBuilder AddCampaignCore(this IMvcBuilder mvcBuilder, CampaignOptionsBase baseOptions) {
        var services = mvcBuilder.Services;
        // Build service provider and get IConfiguration instance.
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        // Try add general settings.
        services.AddGeneralSettings(configuration);
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
        // Post configure MVC options.
        services.PostConfigure<MvcOptions>(options => {
            options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeNames.Application.Json);
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
        // Register application DbContext.
        Action<IServiceProvider, DbContextOptionsBuilder> sqlServerConfiguration = (serviceProvider, builder) => builder.UseSqlServer(configuration.GetConnectionString("MessagesDbConnection"));
        services.AddDbContext<CampaignsDbContext>(baseOptions.ConfigureDbContext ?? sqlServerConfiguration);
        return mvcBuilder;
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
        UseMultiTenancyInternal(options, accessLevel);
        return options;
    }

    /// <summary>Adds multi-tenancy capabilities in the Messages API endpoints.</summary>
    /// <param name="options">Options used to configure the Messages management API feature.</param>
    /// <param name="accessLevel">The minimum access level required.</param>
    public static MessageManagementOptions UseMultiTenancy(this MessageManagementOptions options, int accessLevel) {
        UseMultiTenancyInternal(options, accessLevel);
        return options;
    }

    private static void UseMultiTenancyInternal(CampaignOptionsBase options, int accessLevel) {
        options.Services.AddSingleton<IAuthorizationPolicyProvider, MultitenantCampaignsPolicyProvider>();
        options.Services.Configure<MessageMultitenancyOptions>(options => options.AccessLevel = accessLevel);
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
