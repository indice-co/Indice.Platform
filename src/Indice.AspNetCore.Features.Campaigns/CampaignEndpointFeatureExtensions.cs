using System;
using System.Linq;
using System.Net.Mime;
using FluentValidation.AspNetCore;
using Indice.AspNetCore.Features.Campaigns;
using Indice.AspNetCore.Features.Campaigns.Controllers;
using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Formatters;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.AspNetCore.Mvc.ApplicationModels;
using Indice.AspNetCore.Swagger;
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
        public static IMvcBuilder AddCampaignEndpoints(this IMvcBuilder mvcBuilder, Action<CampaignEndpointOptions> configureAction = null) =>
            mvcBuilder.AddCampaignManagementEndpoints(configureAction)
                      .AddCampaignInboxEndpoints(configureAction);

        /// <summary>
        /// Add Campaigns management API endpoints in the MVC project.
        /// </summary>
        /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
        /// <param name="configureAction">Configuration for several options of Campaigns management API feature.</param>
        public static IMvcBuilder AddCampaignManagementEndpoints(this IMvcBuilder mvcBuilder, Action<CampaignEndpointOptions> configureAction = null) {
            mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new CampaignEndpointFeatureProvider(includeManagementApi: true, includeUserApi: false)));
            var services = mvcBuilder.Services;
            // Configure campaigns system core requirements.
            var campaignsApiOptions = mvcBuilder.AddCampaignCore(configureAction);
            // Post configure MVC options.
            services.PostConfigure<MvcOptions>(options => {
                options.FormatterMappings.SetMediaTypeMappingForFormat("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                options.OutputFormatters.Add(new XlsxCampaignStatisticsOutputFormatter());
                options.Conventions.Add(new ApiPrefixControllerModelConvention(ApiPrefixes.ManagementApi, campaignsApiOptions.ApiPrefix));
            });
            // Register framework services.
            services.AddHttpContextAccessor();
            // Register events.
            services.TryAddTransient<IPlatformEventService, PlatformEventService>();
            // Configure authorization.
            services.AddAuthorizationCore(authOptions => {
                authOptions.AddPolicy(CampaignsApi.Policies.BeCampaignsManager, policy => {
                    policy.AddAuthenticationSchemes(CampaignsApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.HasScopeClaim(campaignsApiOptions.RequiredScope ?? CampaignsApi.Scope) && x.User.CanManageCampaigns());
                });
            });
            return mvcBuilder;
        }

        /// <summary>
        /// Add Campaigns inbox API endpoints in the MVC project.
        /// </summary>
        /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
        /// <param name="configureAction">Configuration for several options of Campaigns inbox API feature.</param>
        public static IMvcBuilder AddCampaignInboxEndpoints(this IMvcBuilder mvcBuilder, Action<CampaignEndpointOptions> configureAction = null) {
            mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new CampaignEndpointFeatureProvider(includeManagementApi: false, includeUserApi: true)));
            var services = mvcBuilder.Services;
            var campaignsApiOptions = mvcBuilder.AddCampaignCore(configureAction);
            // Post configure MVC options.
            services.PostConfigure<MvcOptions>(options => {
                options.Conventions.Add(new ApiPrefixControllerModelConvention(ApiPrefixes.InboxApi, campaignsApiOptions.ApiPrefix));
            });
            // Register custom services.
            services.AddTransient<IInboxService, InboxService>();
            // Configure authorization.
            services.AddAuthorizationCore(authOptions => {
                authOptions.AddPolicy(CampaignsApi.Policies.HaveCampaignsScope, policy => {
                    policy.AddAuthenticationSchemes(CampaignsApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.HasScopeClaim(campaignsApiOptions.RequiredScope ?? CampaignsApi.Scope));
                });
            });
            return mvcBuilder;
        }

        private static CampaignEndpointOptions AddCampaignCore(this IMvcBuilder mvcBuilder, Action<CampaignEndpointOptions> configureAction = null) {
            var services = mvcBuilder.Services;
            // Build service provider and get IConfiguration instance.
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            // Configure options given by the consumer.
            var campaignsApiOptions = new CampaignEndpointOptions(services);
            // Use local files by default. Consumer can override this behavior if he needs to.
            campaignsApiOptions.UseFilesLocal();
            configureAction?.Invoke(campaignsApiOptions);
            campaignsApiOptions.Services = null;
            services.Configure<CampaignEndpointOptions>(options => {
                options.ConfigureDbContext = campaignsApiOptions.ConfigureDbContext;
                options.ApiPrefix = campaignsApiOptions.ApiPrefix;
                options.DatabaseSchema = campaignsApiOptions.DatabaseSchema;
                options.RequiredScope = campaignsApiOptions.RequiredScope;
                options.UserClaimType = campaignsApiOptions.UserClaimType;
            });
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
            mvcBuilder.AddFluentValidation(options => options.RegisterValidatorsFromAssemblyContaining<CampaignsController>());
            // Register framework services.
            services.AddResponseCaching();
            // Register custom services.
            services.TryAddTransient<ICampaignService, CampaignService>();
            services.TryAddTransient<CampaignManager>();
            // Register application DbContext.
            Action<DbContextOptionsBuilder> sqlServerConfiguration = (builder) => builder.UseSqlServer(configuration.GetConnectionString("CampaignsDbConnection"));
            services.AddDbContext<CampaignsDbContext>(campaignsApiOptions.ConfigureDbContext ?? sqlServerConfiguration);
            return campaignsApiOptions;
        }

        /// <summary>
        /// Adds <see cref="IFileService"/> using local file system as the backing store.
        /// </summary>
        /// <param name="options">Options used to configure the Campaigns API feature.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static void UseFilesLocal(this CampaignEndpointOptions options, Action<FileServiceLocal.FileServiceOptions> configure = null) =>
            options.Services.AddFiles(options => options.AddFileSystem(KeyedServiceNames.FileServiceKey, configure));

        /// <summary>
        /// Adds <see cref="IFileService"/> using Azure Blob Storage as the backing store.
        /// </summary>
        /// <param name="options">Options used to configure the Campaigns API feature.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static void UseFilesAzure(this CampaignEndpointOptions options, Action<FileServiceAzureStorage.FileServiceOptions> configure = null) =>
            options.Services.AddFiles(options => options.AddAzureStorage(KeyedServiceNames.FileServiceKey, configure));
    }
}
