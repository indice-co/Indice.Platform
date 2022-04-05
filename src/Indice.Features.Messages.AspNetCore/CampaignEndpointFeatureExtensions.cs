using System;
using System.Linq;
using System.Net.Mime;
using FluentValidation.AspNetCore;
using Indice.AspNetCore.Features.Campaigns;
using Indice.AspNetCore.Features.Campaigns.Formatters;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.AspNetCore.Mvc.ApplicationModels;
using Indice.AspNetCore.Swagger;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
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
    public static class CampaignEndpointFeatureExtensions
    {
        /// <summary>
        /// Add all Campaigns (both management and self-service) API endpoints in the MVC project.
        /// </summary>
        /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
        /// <param name="configureAction">Configuration for several options of Campaigns API feature.</param>
        public static IMvcBuilder AddCampaignEndpoints(this IMvcBuilder mvcBuilder, Action<CampaignEndpointOptions> configureAction = null) {
            var services = mvcBuilder.Services;
            // Configure options.
            var campaignsApiOptions = new CampaignEndpointOptions(services);
            configureAction?.Invoke(campaignsApiOptions);
            return mvcBuilder.AddCampaignManagementEndpoints(options => {
                options.ApiPrefix = campaignsApiOptions.ApiPrefix;
                options.ConfigureDbContext = campaignsApiOptions.ConfigureDbContext;
                options.DatabaseSchema = campaignsApiOptions.DatabaseSchema;
                options.RequiredScope = campaignsApiOptions.RequiredScope;
                options.UserClaimType = campaignsApiOptions.UserClaimType;
            })
            .AddCampaignInboxEndpoints(options => {
                options.ApiPrefix = campaignsApiOptions.ApiPrefix;
                options.ConfigureDbContext = campaignsApiOptions.ConfigureDbContext;
                options.DatabaseSchema = campaignsApiOptions.DatabaseSchema;
                options.RequiredScope = campaignsApiOptions.RequiredScope;
                options.UserClaimType = campaignsApiOptions.UserClaimType;
            });
        }

        /// <summary>
        /// Add Campaigns management API endpoints in the MVC project.
        /// </summary>
        /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
        /// <param name="configureAction">Configuration for several options of Campaigns management API feature.</param>
        public static IMvcBuilder AddCampaignManagementEndpoints(this IMvcBuilder mvcBuilder, Action<CampaignManagementOptions> configureAction = null) {
            mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new CampaignEndpointFeatureProvider(includeManagementApi: true, includeInboxApi: false)));
            var services = mvcBuilder.Services;
            // Configure options.
            var managementApiOptions = new CampaignManagementOptions(services);
            configureAction?.Invoke(managementApiOptions);
            // Configure campaigns system core requirements.
            mvcBuilder.AddCampaignCore(managementApiOptions);
            services.Configure<CampaignManagementOptions>(options => {
                options.ApiPrefix = managementApiOptions.ApiPrefix;
                options.ConfigureDbContext = managementApiOptions.ConfigureDbContext;
                options.DatabaseSchema = managementApiOptions.DatabaseSchema;
                options.RequiredScope = managementApiOptions.RequiredScope;
                options.UserClaimType = managementApiOptions.UserClaimType;
                options.RequiredScope = managementApiOptions.RequiredScope;
            });
            services.AddSingleton(new DatabaseSchemaNameResolver(managementApiOptions.DatabaseSchema));
            // Post configure MVC options.
            services.PostConfigure<MvcOptions>(options => {
                options.Conventions.Add(new ApiPrefixControllerModelConvention(ApiPrefixes.CampaignManagementEndpoints, managementApiOptions.ApiPrefix));
                options.FormatterMappings.SetMediaTypeMappingForFormat("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                options.OutputFormatters.Add(new XlsxCampaignStatisticsOutputFormatter());
            });
            // Register framework services.
            services.AddHttpContextAccessor();
            // Register events.
            services.TryAddTransient<IPlatformEventService, PlatformEventService>();
            services.TryAddTransient<IContactService, ContactService>();
            // Configure authorization.
            services.AddAuthorizationCore(authOptions => {
                authOptions.AddPolicy(CampaignsApi.Policies.BeCampaignsManager, policy => {
                    policy.AddAuthenticationSchemes(CampaignsApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.HasScopeClaim(managementApiOptions.RequiredScope ?? CampaignsApi.Scope) && x.User.CanManageCampaigns());
                });
            });
            return mvcBuilder;
        }

        /// <summary>
        /// Add Campaigns inbox API endpoints in the MVC project.
        /// </summary>
        /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
        /// <param name="configureAction">Configuration for several options of Campaigns inbox API feature.</param>
        public static IMvcBuilder AddCampaignInboxEndpoints(this IMvcBuilder mvcBuilder, Action<CampaignInboxOptions> configureAction = null) {
            mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new CampaignEndpointFeatureProvider(includeManagementApi: false, includeInboxApi: true)));
            var services = mvcBuilder.Services;
            // Configure options.
            var inboxApiOptions = new CampaignInboxOptions(services);
            configureAction?.Invoke(inboxApiOptions);
            mvcBuilder.AddCampaignCore(inboxApiOptions);
            services.Configure<CampaignInboxOptions>(options => {
                options.ApiPrefix = inboxApiOptions.ApiPrefix;
                options.ConfigureDbContext = inboxApiOptions.ConfigureDbContext;
                options.DatabaseSchema = inboxApiOptions.DatabaseSchema;
                options.RequiredScope = inboxApiOptions.RequiredScope;
                options.UserClaimType = inboxApiOptions.UserClaimType;
                options.RequiredScope = inboxApiOptions.RequiredScope;
            });
            services.AddSingleton(new DatabaseSchemaNameResolver(inboxApiOptions.DatabaseSchema));
            // Post configure MVC options.
            services.PostConfigure<MvcOptions>(options => {
                options.Conventions.Add(new ApiPrefixControllerModelConvention(ApiPrefixes.CampaignInboxEndpoints, inboxApiOptions.ApiPrefix));
            });
            // Register custom services.
            services.AddTransient<IInboxService, InboxService>();
            // Configure authorization.
            services.AddAuthorizationCore(authOptions => {
                authOptions.AddPolicy(CampaignsApi.Policies.HaveCampaignsScope, policy => {
                    policy.AddAuthenticationSchemes(CampaignsApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.HasScopeClaim(inboxApiOptions.RequiredScope ?? CampaignsApi.Scope));
                });
            });
            return mvcBuilder;
        }

        private static IMvcBuilder AddCampaignCore(this IMvcBuilder mvcBuilder, CampaignOptionsBase baseOptions) {
            var services = mvcBuilder.Services;
            baseOptions.UseFilesLocal();
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
            services.TryAddTransient<CampaignManager>();
            // Register application DbContext.
            Action<DbContextOptionsBuilder> sqlServerConfiguration = (builder) => builder.UseSqlServer(configuration.GetConnectionString("CampaignsDbConnection"));
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
