using System;
using System.Linq;
using System.Net.Mime;
using FluentValidation.AspNetCore;
using Indice.AspNetCore.Mvc.ApplicationModels;
using Indice.AspNetCore.Swagger;
using Indice.Features.Messages.AspNetCore;
using Indice.Features.Messages.AspNetCore.Mvc.Formatters;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Manager;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;
using Indice.Security;
using Indice.Serialization;
using Indice.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods on <see cref="IMvcBuilder"/> for configuring Campaigns API feature.
    /// </summary>
    public static class MessageFeatureExtensions
    {
        /// <summary>
        /// Add all Campaigns (both management and self-service) API endpoints in the MVC project.
        /// </summary>
        /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
        /// <param name="configureAction">Configuration for several options of Campaigns API feature.</param>
        public static IMvcBuilder AddMessageEndpoints(this IMvcBuilder mvcBuilder, Action<MessageEndpointOptions> configureAction = null) {
            var services = mvcBuilder.Services;
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

        /// <summary>
        /// Add Campaigns management API endpoints in the MVC project.
        /// </summary>
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
            services.TryAddTransient<IPlatformEventService, PlatformEventService>();
            services.TryAddTransient<IContactService, ContactService>();
            services.TryAddTransient<ITemplateService, TemplateService>();
            services.TryAddTransient<ICampaignAttachmentService, CampaignAttachmentService>();
            services.TryAddTransient<NotificationsManager>();
            services.TryAddTransient<IDistributionListService, DistributionListService>();
            services.TryAddTransient<IMessageService, MessageService>();
            services.TryAddTransient<ICampaignService, CampaignService>();
            services.TryAddTransient<IMessageTypeService, MessageTypeService>();
            services.TryAddTransient<CreateCampaignRequestValidator>();
            services.TryAddTransient<CreateMessageTypeRequestValidator>();
            // Configure authorization.
            services.AddAuthorizationCore(authOptions => {
                authOptions.AddPolicy(MessagesApi.Policies.BeCampaignManager, policy => {
                    policy.AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.HasScopeClaim(apiOptions.RequiredScope ?? MessagesApi.Scope) && x.User.CanManageCampaigns());
                });
            });
            return mvcBuilder;
        }

        /// <summary>
        /// Add Campaigns inbox API endpoints in the MVC project.
        /// </summary>
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
            services.AddTransient<IInboxService, InboxService>();
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
            mvcBuilder.AddFluentValidation(options => options.RegisterValidatorsFromAssemblyContaining<CreateCampaignRequestValidator>());
            // Register framework services.
            services.AddResponseCaching();
            // Register custom services.
            services.TryAddTransient<ICampaignService, CampaignService>();
            services.TryAddTransient<IMessageTypeService, MessageTypeService>();
            services.TryAddTransient<IDistributionListService, DistributionListService>();
            // Register application DbContext.
            Action<DbContextOptionsBuilder> sqlServerConfiguration = (builder) => builder.UseSqlServer(configuration.GetConnectionString("MessagesDbConnection"));
            services.AddDbContext<CampaignsDbContext>(baseOptions.ConfigureDbContext ?? sqlServerConfiguration);
            return mvcBuilder;
        }

        /// <summary>
        /// Adds <see cref="IFileService"/> using local file system as the backing store.
        /// </summary>
        /// <param name="options">Options used to configure the Campaigns API feature.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static void UseFilesLocal(this CampaignOptionsBase options, Action<FileServiceLocalOptions> configure = null) => 
            options.Services.AddFiles(options => options.AddFileSystem(KeyedServiceNames.FileServiceKey, configure));

        /// <summary>
        /// Adds <see cref="IFileService"/> using Azure Blob Storage as the backing store.
        /// </summary>
        /// <param name="options">Options used to configure the Campaigns API feature.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static void UseFilesAzure(this CampaignOptionsBase options, Action<FileServiceAzureOptions> configure = null) =>
            options.Services.AddFiles(options => options.AddAzureStorage(KeyedServiceNames.FileServiceKey, configure));
    }
}
