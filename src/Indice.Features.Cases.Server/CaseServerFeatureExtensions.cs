using FluentValidation;
using FluentValidation.AspNetCore;
using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Authorization;
using Indice.Features.Cases.Server.Endpoints;
using Indice.Features.Cases.Server.Endpoints.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Adds all services needed to configure Case management server.
/// </summary>
public static class CaseServerFeatureExtensions
{
    /// <summary>Adds the case server dependencies.</summary>
    /// <param name="builder">Specifies the <see cref="IHostApplicationBuilder"/> to configure</param>
    /// <param name="setupAction">The setup action.</param>
    /// <returns>The <see cref="IHostApplicationBuilder"/> for further configuration.</returns>
    public static IHostApplicationBuilder AddCaseServer(
        this IHostApplicationBuilder builder,
        Action<CaseServerOptions>? setupAction = null
    ) {
        // Configure options given by the consumer.
        var serverOptions = new CaseServerOptions(builder.Services);
        setupAction?.Invoke(serverOptions);
        builder.Services.AddCasesCore(options => {
            options.ConfigureDbContext = serverOptions.ConfigureDbContext;
            options.DatabaseSchema = serverOptions.DatabaseSchema;
            options.RequiredScope = serverOptions.RequiredScope;
            options.ByPassAccessRulesForElevatedUsers = serverOptions.ByPassAccessRulesForElevatedUsers;
            options.UserClaimType = serverOptions.UserClaimType;
            options.GroupIdClaimType = serverOptions.GroupIdClaimType;
            options.ReferenceNumberEnabled = serverOptions.ReferenceNumberEnabled;
        });
        // must run last in order not to override any explicit service declarations.
        builder.Services.AddCasesManagement(options => {
            options.ConfigureDbContext = serverOptions.ConfigureDbContext;
            options.DatabaseSchema = serverOptions.DatabaseSchema;
            options.RequiredScope = serverOptions.RequiredScope;
            options.UserClaimType = serverOptions.UserClaimType;
            options.GroupIdClaimType = serverOptions.GroupIdClaimType;
            options.ReferenceNumberEnabled = serverOptions.ReferenceNumberEnabled;
            options.ByPassAccessRulesForElevatedUsers = serverOptions.ByPassAccessRulesForElevatedUsers;
        });
        builder.Services.Configure<CaseServerOptions>(options => {
            options.PathPrefix = serverOptions.PathPrefix;
            options.DatabaseSchema = serverOptions.DatabaseSchema;
            options.RequiredScope = serverOptions.RequiredScope;
            options.UserClaimType = serverOptions.UserClaimType;
            options.GroupIdClaimType = serverOptions.GroupIdClaimType;
            options.GroupName = serverOptions.GroupName;
            options.ConfigureLimitUpload = serverOptions.ConfigureLimitUpload;
            options.ByPassAccessRulesForElevatedUsers = serverOptions.ByPassAccessRulesForElevatedUsers;
        });
        builder.Services.AddLimitUpload(serverOptions.ConfigureLimitUpload);
        builder.Services.AddTransient<IAuthorizationHandler, CasesAccessHandler>();
        builder.Services.AddFluentValidationAutoValidation()
                       .AddValidatorsFromAssemblyContaining<AddAccessRuleRequestValidator>()
                       .AddFluentValidationClientsideAdapters();
        return builder;
    }

    /// <summary>Adds all case server endpoints.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> for further configuration.</returns>
    public static IEndpointRouteBuilder MapCases(this IEndpointRouteBuilder routes) {
        // my account
        routes.MapMyCases();
        routes.MapMyCaseTypes();
        // management endpoints
        routes.MapAdminCases();
        routes.MapAdminAttachments();
        routes.MapAdminCaseTypes();
        routes.MapAdminCheckpointTypes();
        routes.MapAdminIntegration();
        routes.MapAdminNotifications();
        routes.MapAdminQueries();
        routes.MapAdminReports();
        routes.MapAdminWorkflowInvoker();
        routes.MapLookup();
        routes.MapAdminAccessRules();
        routes.MapAdminCaseData();
        return routes;
    }
}

