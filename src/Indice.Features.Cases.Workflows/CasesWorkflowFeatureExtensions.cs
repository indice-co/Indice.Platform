using Elsa;
using Elsa.Activities.Http.Services;
using Elsa.Activities.UserTask.Extensions;
using Elsa.Persistence.EntityFramework.Core;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Retention.Extensions;
using Elsa.Server.Api.Extensions;
using Elsa.Server.Api.Mapping;
using Elsa.Server.Api.Services;
using Elsa.Server.Api;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Workflows;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval;
using Indice.Features.Cases.Workflows.Data;
using Indice.Features.Cases.Workflows.Interfaces;
using Indice.Features.Cases.Workflows.Services;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configureing the CasesWorkflow Feature.
/// </summary>
public static class CasesWorkflowFeatureExtensions
{
    /// <summary>Add case management workflow configuratiuon.</summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="configureAction">The optional configuration action.</param>
    public static IServiceCollection AddCasesWorkflow(this IServiceCollection services, Action<CasesWorkflowOptions>? configureAction = null) {
        // Configure options given by the consumer.
        var workflowOptions = new CasesWorkflowOptions(services);
        configureAction?.Invoke(workflowOptions);
        services.Configure<CasesWorkflowOptions>(options => {
            options.ConfigureDbContext = workflowOptions.ConfigureDbContext;
            options.ConfigureRetentionServices = workflowOptions.ConfigureRetentionServices;
            options.ConfigureSmtp = workflowOptions.ConfigureSmtp;
            options.GetWorkflowAssembly = workflowOptions.GetWorkflowAssembly;
            options.RetentionServicesEnabled = workflowOptions.RetentionServicesEnabled;
            options.RetentionSpecificationFilter = workflowOptions.RetentionSpecificationFilter;
            options.ServerBasePath = workflowOptions.ServerBasePath;
            options.ServerBaseUrl = workflowOptions.ServerBaseUrl;
        });

        //services.TryAddTransient<CasesMessageDescriber>();
        services.AddWorkflowInternal(workflowOptions);
        return services;
    }


    /// <summary>Add workflow services to the case management.</summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="casesWorkflowOptions">The configuration options.</param>
    internal static IServiceCollection AddWorkflowInternal(
        this IServiceCollection services,
        CasesWorkflowOptions casesWorkflowOptions) {
        // db initializer
        var configureDatabase = casesWorkflowOptions.ConfigureDbContext ?? new Action<IServiceProvider, DbContextOptionsBuilder>((sp, ef) => ef.UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString("WorkflowDb")));
        services.AddHostedService<CasesWorkflowDbInitializerHostedService>();
        services.AddDbContextFactory<ElsaContext>(configureDatabase);

        services.AddElsa(elsa => {
            elsa.UseEntityFrameworkPersistence(configureDatabase, autoRunMigrations: false)
            .AddQuartzTemporalActivities()
            .AddHttpActivities(http => {
                http.HttpEndpointAuthorizationHandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<AuthenticationBasedHttpEndpointAuthorizationHandler>;
                if (casesWorkflowOptions.ServerBaseUrl is { } baseUrl) {
                    http.BaseUrl = new Uri(baseUrl);
                }
                if (casesWorkflowOptions.ServerBasePath is { } basePath) {
                    http.BasePath = basePath;
                }
            })
            .AddEmailActivities(casesWorkflowOptions.ConfigureSmtp)
            .AddUserTaskActivities()
            .AddActivitiesFrom(typeof(CasesWorkflowOptions).Assembly);

            // Register consumer assembly
            var workflowAssembly = casesWorkflowOptions.GetWorkflowAssembly?.Invoke();
            if (workflowAssembly != null) {
                elsa.AddWorkflowsFrom(workflowAssembly);
                elsa.AddActivitiesFrom(workflowAssembly);
            }
        });

        if (casesWorkflowOptions.RetentionServicesEnabled) {
            services.AddRetentionServices(options => {
                options.BatchSize = 100;
                options.TimeToLive = Duration.FromDays(30);
                options.SweepInterval = Duration.FromDays(4);
                if (casesWorkflowOptions.RetentionSpecificationFilter is not null) {
                    options.ConfigureSpecificationFilter(casesWorkflowOptions.RetentionSpecificationFilter);
                }
                casesWorkflowOptions.ConfigureRetentionServices?.Invoke(options);
            });
        }

        // Elsa API endpoints. - Fixes Swagger UI when commented - commented while using minimal APIs
        services.AddElsaApiEndpointsInternal(); //this breaks the swagger UI

        // Register Indices' bookmarks
        services.AddBookmarkProvidersFrom(typeof(AwaitApprovalBookmark).Assembly);

        var workflowAssembly = casesWorkflowOptions.GetWorkflowAssembly?.Invoke();
        // Register bookmarks from consumer assembly
        if (workflowAssembly != null) {
            services.AddBookmarkProvidersFrom(workflowAssembly);
        }

        // Register Custom Services
        // Workflow integration
        services.TryAddScoped<IAwaitApprovalInvoker, AwaitApprovalInvoker>();
        services.TryAddScoped<IAwaitEditInvoker, AwaitEditInvoker>();
        services.TryAddScoped<IAwaitAssignmentInvoker, AwaitAssignmentInvoker>();
        services.TryAddScoped<IAwaitActionInvoker, AwaitActionInvoker>();
        services.AddScoped<ICasesWorkflowManager, CasesWorkflowManagerElsa>();
        //
        // TODO: Should remove dependecies to core services.
        // Here there are missing service registrations related to
        // accessing the CasesDbContext directly via the cases core services
        // We should refactor the code to use a HttpClient instead of direct db access
        // We can track down these dependencies by inspecting code inside of custom activities.

        return services;
    }

    /// <summary>Add workflow middleware and activities to http pipeline.</summary>
    /// <param name="app"></param>
    public static void UseCasesWorkflow(this IApplicationBuilder app) {
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
    /// because it makes use of the <strong>OpenIdConnect</strong> scheme in order to authorize a visiting user</remarks>
    public static IServiceCollection AddCasesWorkflowAuthoriationPolicy(this IServiceCollection services, Action<AuthorizationPolicyBuilder>? configurePolicy = null) {
        configurePolicy ??= policy => policy
                .AddAuthenticationSchemes("Bearer", "OpenIdConnect")
                .RequireAuthenticatedUser()
                .RequireAssertion(x => x.User.IsAdmin() || x.User.IsSystemClient());

        services.AddAuthorization(authOptions => {
            authOptions.AddPolicy(WorkflowPolicy, configurePolicy);
        });

        services.PostConfigure<MvcOptions>(options => {
            options.Conventions.Add(new GroupWorkflowActionsConvention());
            options.Conventions.Add(new AddWorkflowAuthorizeFiltersConvention());
        });

        services.PostConfigure<RazorPagesOptions>(options => {
            options.Conventions.Add(new AddWorkflowAuthorizeFiltersConvention());
        });
        return services;
    }


    internal class GroupWorkflowActionsConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller) {
            // This is for ELSA API
            if (controller.DisplayName.Contains("elsa", StringComparison.OrdinalIgnoreCase)) {
                controller.ApiExplorer.IsVisible = false;
                controller.ApiExplorer.GroupName = "workflow";
            }
        }
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


    internal static IServiceCollection AddElsaApiEndpointsInternal(this IServiceCollection services) {
        //Don't set Newtonsoft globally
        services.AddControllers(options => {
            //Use this conventions to set ElsaNewtonsoftJsonConvention to all controllers in Elsa.Server.Api
            options.Conventions.Add(new ElsaNewtonsoftJsonConvention());
            options.Conventions.Add(new GroupWorkflowActionsConvention());
        });
        services.AddRouting(options => { options.LowercaseUrls = true; });

        //services.AddVersionedApiExplorer(o => {
        //    o.GroupNameFormat = "'v'VVV";
        //    o.SubstituteApiVersionInUrl = true;
        //});

        services.AddApiVersioning(
            options => {
                options.ReportApiVersions = true;
                options.DefaultApiVersion = ApiVersion.Default;
                options.AssumeDefaultVersionWhenUnspecified = true;
            });

        services
            .AddSingleton<ConnectionConverter>()
            .AddSingleton<ActivityBlueprintConverter>()
            .AddScoped<IWorkflowBlueprintMapper, WorkflowBlueprintMapper>()
            .AddSingleton<IEndpointContentSerializerSettingsProvider, EndpointContentSerializerSettingsProvider>()
            .AddAutoMapperProfile<AutoMapperProfile>()
            .AddSignalR();
        services.AddMvc(options =>
        {
            //Use this conventions to set ElsaNewtonsoftJsonConvention to all controllers in Elsa.Server.Api
            options.Conventions.Add(new ElsaNewtonsoftJsonConvention());
            options.Conventions.Add(new GroupWorkflowActionsConvention());
        });
        return services;
    }
}
