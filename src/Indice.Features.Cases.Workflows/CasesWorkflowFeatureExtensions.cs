using Elsa.Activities.Http.Services;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Retention.Contracts;
using Elsa.Retention.Extensions;
using Elsa.Retention.Specifications;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval;
using Indice.Features.Cases.Workflows.Interfaces;
using Indice.Features.Cases.Workflows.Services;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using Elsa;
using NodaTime;
using Elsa.Activities.UserTask.Extensions;

namespace Indice.Features.Cases.Workflows;

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

        var configureDatabase = casesWorkflowOptions.ConfigureDbContext ?? new Action<IServiceProvider, DbContextOptionsBuilder>((sp, ef) => ef.UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString("WorkflowDb")));

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
        services.AddElsaApiEndpoints();

        // For Dashboard.
        services.AddRazorPages();

        // Register Indices' bookmarks
        services.AddBookmarkProvidersFrom(typeof(AwaitApprovalBookmark).Assembly);

        var workflowAssembly = casesWorkflowOptions.GetWorkflowAssembly?.Invoke();
        // Register bookmarks from consumer assembly
        if (workflowAssembly != null) {
            services.AddBookmarkProvidersFrom(workflowAssembly);
        }

        // Register Custom Services
        // Workflow integration
        services.AddScoped<IAwaitApprovalInvoker, AwaitApprovalInvoker>();
        services.AddScoped<IAwaitEditInvoker, AwaitEditInvoker>();
        services.AddScoped<IAwaitAssignmentInvoker, AwaitAssignmentInvoker>();
        services.AddScoped<IAwaitActionInvoker, AwaitActionInvoker>();
        services.AddScoped<ICasesWorkflowManager, CasesWorkflowManagerElsa>();

        return services;
    }
}
