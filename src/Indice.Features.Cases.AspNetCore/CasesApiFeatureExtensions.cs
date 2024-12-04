using System.Reflection;
using Elsa;
using Elsa.Activities.Http.Services;
using Elsa.Activities.UserTask.Extensions;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Retention.Contracts;
using Elsa.Retention.Extensions;
using Elsa.Retention.Specifications;
using Indice.Security;
using Indice.AspNetCore.Configuration;
using Indice.AspNetCore.Mvc.ApplicationModels;
using Indice.Features.Cases.Converters;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval;
using Indice.Features.Cases.Workflows.Interfaces;
using Indice.Features.Cases.Workflows.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using Indice.Features.Cases.Services;
using Indice.Features.Cases.Core.Events;
using Indice.Features.Cases.Events;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Indice.Features.Cases;

/// <summary>Register Case Api Features.</summary>
public static class CasesApiFeatureExtensions
{
    /// <summary>
    /// Adds the default limit upload options for Cases API.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureLimitUpload"></param>
    /// <returns></returns>
    internal static IServiceCollection AddLimitUploadOptions(this IServiceCollection services, Action<LimitUploadOptions>? configureLimitUpload = null) {
        services.AddLimitUpload(configureLimitUpload ?? (options => {
            options.DefaultMaxFileSizeBytes = 6 * 1024 * 1024; // 6 megabytes
            options.DefaultAllowedFileExtensions = [".pdf", ".jpeg", ".jpg", ".tif", ".tiff"];
        }));
        return services;
    }

    /// <summary>Add case management Api endpoints for Customer (api/my prefix).</summary>
    /// <param name="mvcBuilder">The <see cref="IMvcBuilder"/>.</param>
    /// <param name="configureAction">The <see cref="IConfiguration"/>.</param>
    public static IMvcBuilder AddCasesApiEndpoints(this IMvcBuilder mvcBuilder, Action<MyCasesApiOptions>? configureAction = null) {
        // Add
        mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new CasesApiFeatureProviderMyCases()));
        var services = mvcBuilder.Services;

        // Build service provider and get IConfiguration instance.
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var environment = serviceProvider.GetRequiredService<IHostEnvironment>();

        // Try add general settings.
        services.AddGeneralSettings(configuration);

        // This lines resolves the CaseData dynamic deserialization from SystemText
        services.AddMvc()
            .AddNewtonsoftJson(x => x.SerializerSettings.Converters.Add(new SystemTextConverter()));

        // Configure options given by the consumer.
        var casesMyOptions = new MyCasesApiOptions();
        configureAction?.Invoke(casesMyOptions);
        services.Configure<MyCasesApiOptions>(options => {
            options.ConfigureDbContext = casesMyOptions.ConfigureDbContext;
            options.DatabaseSchema = casesMyOptions.DatabaseSchema;
            options.UserClaimType = casesMyOptions.UserClaimType;
            options.GroupIdClaimType = casesMyOptions.GroupIdClaimType;
            options.ReferenceNumberEnabled = casesMyOptions.ReferenceNumberEnabled;
            options.RequiredScope = casesMyOptions.RequiredScope;
            // api spesifics
            options.ApiPrefix = casesMyOptions.ApiPrefix;
            options.GroupName = casesMyOptions.GroupName;
            options.ConfigureLimitUpload = casesMyOptions.ConfigureLimitUpload;
        });

        services.AddLimitUploadOptions(casesMyOptions.ConfigureLimitUpload);

        // Post configure MVC options.
        services.PostConfigure<MvcOptions>(options => {
            options.Conventions.Add(new ApiPrefixControllerModelConvention(ApiPrefixes.MyCasesApiTemplatePrefixPlaceholder, casesMyOptions.ApiPrefix ?? "api"));
            options.Conventions.Add(new ApiGroupNameControllerModelConvention(ApiGroups.MyCasesApiGroupNamePlaceholder, casesMyOptions.GroupName));
        });

        // Register framework services.
        services.AddHttpContextAccessor();

        // Register internal handlers
        services.AddCaseEventHandler<CaseSubmittedEvent, StartWorkflowHandler>();
        // must run last in order not to override any explicit service declarations.
        services.AddCasesCore(options => {
            options.ConfigureDbContext = casesMyOptions.ConfigureDbContext;
            options.DatabaseSchema = casesMyOptions.DatabaseSchema;
            options.UserClaimType = casesMyOptions.UserClaimType;
            options.GroupIdClaimType = casesMyOptions.GroupIdClaimType;
            options.ReferenceNumberEnabled = casesMyOptions.ReferenceNumberEnabled;
        });
        return mvcBuilder;
    }


    /// <summary>Add case management Api endpoints for manage cases from back-office (api/manage prefix).</summary>
    /// <param name="mvcBuilder">The <see cref="IMvcBuilder"/>.</param>
    /// <param name="configureAction">The <see cref="IConfiguration"/>.</param>
    public static IMvcBuilder AddAdminCasesApiEndpoints(this IMvcBuilder mvcBuilder, Action<AdminCasesApiOptions>? configureAction = null) {
        // Add
        mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new CasesApiFeatureProviderAdminCases()));
        var services = mvcBuilder.Services;

        // Build service provider and get IConfiguration instance.
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();


        var casesAdminOptions = new AdminCasesApiOptions();
        configureAction?.Invoke(casesAdminOptions);
        services.Configure<AdminCasesApiOptions>(options => {
            options.ConfigureDbContext = casesAdminOptions.ConfigureDbContext;
            options.DatabaseSchema = casesAdminOptions.DatabaseSchema;
            options.UserClaimType = casesAdminOptions.UserClaimType;
            options.GroupIdClaimType = casesAdminOptions.GroupIdClaimType;
            options.ReferenceNumberEnabled = casesAdminOptions.ReferenceNumberEnabled;
            options.RequiredScope = casesAdminOptions.RequiredScope;
            // api spesifics
            options.ApiPrefix = casesAdminOptions.ApiPrefix;
            options.GroupName = casesAdminOptions.GroupName;
            options.ConfigureLimitUpload = casesAdminOptions.ConfigureLimitUpload;
        });
        services.AddLimitUploadOptions(casesAdminOptions.ConfigureLimitUpload);
        
        // Post configure MVC options.
        services.PostConfigure<MvcOptions>(options => {
            options.Conventions.Add(new ApiPrefixControllerModelConvention(ApiPrefixes.CasesApiTemplatePrefixPlaceholder, casesAdminOptions.ApiPrefix ?? "api"));
            options.Conventions.Add(new ApiGroupNameControllerModelConvention(ApiGroups.CasesApiGroupNamePlaceholder, casesAdminOptions.GroupName));
        });

        // Register framework services.
        services.AddHtmlRenderingEngineRazorMvc();// used by the CasesTemplate service
        services.AddHttpContextAccessor();
        services.AddGeneralSettings(configuration);
        services.AddSmsServiceYubotoOmni(configuration)
            .AddViberServiceYubotoOmni(configuration)
            .AddEmailServiceSparkPost(configuration)
            .WithMvcRazorRendering();

        // Register internal handlers
        services.AddCaseEventHandler<CaseSubmittedEvent, StartWorkflowHandler>();
        // must run last in order not to override any explicit service declarations.
        services.AddCasesManagement(options => {
            options.ConfigureDbContext = casesAdminOptions.ConfigureDbContext;
            options.DatabaseSchema = casesAdminOptions.DatabaseSchema;
            options.UserClaimType = casesAdminOptions.UserClaimType;
            options.GroupIdClaimType = casesAdminOptions.GroupIdClaimType;
            options.ReferenceNumberEnabled = casesAdminOptions.ReferenceNumberEnabled;
        });

        services.AddDecorator<ICaseApprovalService, WorkflowCaseApprovalService>(); // this solves the mixed nature of the service

        return mvcBuilder;
    }

    /// <summary>Add workflow services to the case management.</summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    /// <param name="workflowAssembly">The assembly with the workflow activities and definitions to register.</param>
    /// <param name="retentionSpecificationFilter">Override the specification filter that will select the workflows for deletion. If the value is null the default <see cref="CompletedWorkflowFilterSpecification"/> will be used.</param>
    public static void AddWorkflow(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly workflowAssembly,
        IRetentionSpecificationFilter? retentionSpecificationFilter = null) {
        services.AddElsa(elsa => {
            elsa.UseEntityFrameworkPersistence(ef => ef.UseSqlServer(configuration.GetConnectionString("WorkflowDb")), false)
                .AddQuartzTemporalActivities()
                .AddHttpActivities(http => {
                    http.HttpEndpointAuthorizationHandlerFactory =
                        ActivatorUtilities.GetServiceOrCreateInstance<AuthenticationBasedHttpEndpointAuthorizationHandler>;
                    if (configuration["Elsa:Server:BaseUrl"] is { } baseUrl) {
                        http.BaseUrl = new Uri(baseUrl);
                    }
                    if (configuration["Elsa:Server:BasePath"] is { } basePath) {
                        http.BasePath = basePath;
                    }
                })
                .AddEmailActivities(configuration.GetSection("Elsa").GetSection("Smtp").Bind)
                .AddUserTaskActivities()
                .AddActivitiesFrom(typeof(BaseCaseActivity).Assembly);

            // Register consumer assembly
            if (workflowAssembly != null) {
                elsa.AddWorkflowsFrom(workflowAssembly);
                elsa.AddActivitiesFrom(workflowAssembly);
            }
        });

        var cleanUpOptions = configuration.GetSection("Elsa").GetSection("CleanUpOptions");
        if (cleanUpOptions.GetSection("Enabled").Get<bool?>() ?? true) {
            services.AddRetentionServices(options => {
                options.BatchSize = cleanUpOptions.GetSection("BatchSize").Get<int?>() ?? 100;
                options.TimeToLive = Duration.FromDays(cleanUpOptions.GetSection("TimeToLiveInDays").Get<int?>() ?? 30);
                options.SweepInterval = Duration.FromDays(cleanUpOptions.GetSection("SweepIntervalInHours").Get<int?>() ?? 4);
                if (retentionSpecificationFilter is not null) {
                    options.ConfigureSpecificationFilter(retentionSpecificationFilter);
                }
            });
        }

        // Elsa API endpoints. - Fixes Swagger UI when commented - commented while using minimal APIs
        //services.AddElsaApiEndpoints();

        // For Dashboard.
        services.AddRazorPages();

        // Register Indices' bookmarks
        services.AddBookmarkProvidersFrom(typeof(AwaitApprovalBookmark).Assembly);

        // Register bookmarks from consumer assembly
        if (workflowAssembly != null) {
            services.AddBookmarkProvidersFrom(workflowAssembly);
        }

        // Register Custom Services
        services.AddScoped<IAwaitApprovalInvoker, AwaitApprovalInvoker>();
        services.AddScoped<IAwaitEditInvoker, AwaitEditInvoker>();
        services.AddScoped<IAwaitAssignmentInvoker, AwaitAssignmentInvoker>();
        services.AddScoped<IAwaitActionInvoker, AwaitActionInvoker>();
    }

    /// <summary>Add workflow services to middleware.</summary>
    /// <param name="app"></param>
    public static void UseWorkflow(this IApplicationBuilder app) {
        app.UseHttpActivities();
    }

    internal const string WorkflowPolicy = "WorkflowPolicy";
    /// <summary>
    /// Adds a default security policy for Elsa Controllers and Razor Pages.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configurePolicy">Override the default policy</param>
    /// <returns>The service collection for further configuration</returns>
    /// <remarks>Should be used in conjunction with the <strong>AddAuthentication().AddOpenIdConnect()</strong> 
    /// because it makes use of the <seealso cref="OpenIdConnectDefaults.AuthenticationScheme"/> in order to authorize a visiting user</remarks>
    public static IServiceCollection AddWorkflowAuthoriationPolicy(this IServiceCollection services, Action<AuthorizationPolicyBuilder>? configurePolicy = null) {
        configurePolicy ??= policy => policy
                .AddAuthenticationSchemes(CasesApiConstants.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireAssertion(x => x.User.IsAdmin() || x.User.IsSystemClient());

        services.AddAuthorization(authOptions => {
            authOptions.AddPolicy(WorkflowPolicy, configurePolicy);
        });

        services.PostConfigure<MvcOptions>(options => {
            options.Conventions.Add(new AddWorkflowAuthorizeFiltersConvention());
        });

        services.PostConfigure<RazorPagesOptions>(options => {
            options.Conventions.Add(new AddWorkflowAuthorizeFiltersConvention());
        });
        return services;
    }

    internal class AddWorkflowAuthorizeFiltersConvention : IControllerModelConvention, IPageApplicationModelConvention
    {
        public void Apply(ControllerModel controller) {
            // This is for ELSA API
            if (controller.DisplayName.Contains("elsa", StringComparison.OrdinalIgnoreCase)) {
                controller.Filters.Add(new AuthorizeFilter(WorkflowPolicy));
            }
        }

        public void Apply(PageApplicationModel model) {
            // This is for ELSA razor pages
            if (model.HandlerType.Namespace!.Contains("elsa", StringComparison.OrdinalIgnoreCase)) {
                model.Filters.Add(new AuthorizeFilter(WorkflowPolicy)); // razor pages are only elsa
            }
        }
    }
}
