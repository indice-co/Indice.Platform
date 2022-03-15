using System;
using System.Linq;
using System.Net.Mime;
using FluentValidation.AspNetCore;
using Indice.AspNetCore.Features.Campaigns;
using Indice.AspNetCore.Features.Campaigns.Controllers;
using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.AspNetCore.Features.Campaigns.Formatters;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.AspNetCore.Mvc.ApplicationModels;
using Indice.AspNetCore.Swagger;
using Indice.Extensions;
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
    public static class CampaignsApiFeatureExtensions
    {
        /// <summary>
        /// Add the Campaigns API endpoints in the MVC project.
        /// </summary>
        /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
        /// <param name="configureAction">Configuration for several options of Campaigns API feature.</param>
        public static IMvcBuilder AddCampaignsApiEndpoints(this IMvcBuilder mvcBuilder, Action<CampaignsApiOptions> configureAction = null) {
            mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new CampaignsApiFeatureProvider()));
            var services = mvcBuilder.Services;
            // Build service provider and get IConfiguration instance.
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            // Try add general settings.
            services.AddGeneralSettings(configuration);
            // Configure options given by the consumer.
            var campaignsApiOptions = new CampaignsApiOptions {
                Services = services
            };
            // Use local files by default. Consumer can override this behavior if he needs to.
            campaignsApiOptions.UseFilesLocal();
            configureAction?.Invoke(campaignsApiOptions);
            campaignsApiOptions.Services = null;
            services.Configure<CampaignsApiOptions>(options => {
                options.ApiPrefix = campaignsApiOptions.ApiPrefix;
                options.ConfigureDbContext = campaignsApiOptions.ConfigureDbContext;
                options.DatabaseSchema = campaignsApiOptions.DatabaseSchema;
                options.RequiredScope = campaignsApiOptions.RequiredScope;
                options.UserClaimType = campaignsApiOptions.UserClaimType;
            });
            // Post configure MVC options.
            services.PostConfigure<MvcOptions>(options => {
                options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeNames.Application.Json);
                options.FormatterMappings.SetMediaTypeMappingForFormat("xlsx", FileExtensions.GetMimeType("xlsx"));
                options.OutputFormatters.Add(new XlsxCampaignStatisticsOutputFormatter());
                options.Conventions.Add(new ApiPrefixControllerModelConvention("[campaignsApiPrefix]", campaignsApiOptions.ApiPrefix ?? "api"));
            });
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
            // Register framework services.
            services.AddHttpContextAccessor();
            services.AddResponseCaching();
            // Register custom services.
            services.AddTransient<ICampaignService, CampaignService>();
            services.AddTransient<IMessagesService, MessagesService>();
            services.AddTransient<CampaignManager>();
            // Register validators.
            mvcBuilder.AddFluentValidation(options => options.RegisterValidatorsFromAssemblyContaining<CampaignsController>());
            // Register application DbContext.
            if (campaignsApiOptions.ConfigureDbContext != null) {
                services.AddDbContext<CampaignsDbContext>(campaignsApiOptions.ConfigureDbContext);
            } else {
                services.AddDbContext<CampaignsDbContext>((builder) => builder.UseSqlServer(configuration.GetConnectionString("CampaignsDbConnection")));
            }
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
        /// Adds <see cref="IFileService"/> using local file system as the backing store.
        /// </summary>
        /// <param name="options">Options used to configure the Campaigns API feature.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static void UseFilesLocal(this CampaignsApiOptions options, Action<FileServiceLocal.FileServiceOptions> configure = null) =>
            options.Services.AddFiles(options => options.AddFileSystem(CampaignsApi.FileServiceKey, configure));

        /// <summary>
        /// Adds <see cref="IFileService"/> using Azure Blob Storage as the backing store.
        /// </summary>
        /// <param name="options">Options used to configure the Campaigns API feature.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static void UseFilesAzure(this CampaignsApiOptions options, Action<FileServiceAzureStorage.FileServiceOptions> configure = null) =>
            options.Services.AddFiles(options => options.AddAzureStorage(CampaignsApi.FileServiceKey, configure));

        /// <summary>
        /// Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.
        /// </summary>
        /// <param name="options">Options used to configure the Campaigns API feature.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static void UseEventDispatcherAzure(this CampaignsApiOptions options, Action<IServiceProvider, EventDispatcherAzureOptions> configure = null) => 
            options.Services.AddEventDispatcherAzure(CampaignsApi.EventDispatcherAzureServiceKey, configure);
    }
}
