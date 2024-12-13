using Indice.Features.Cases.Server.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace Indice.Features.Cases.Server;

/// <summary>Provider for registering admin-cases endpoints.</summary>
public static class CasesApiFeatureProviderAdminCases
{
    /// <summary>Registers the admin-cases features.</summary>
    public static void MapAdminCasesEndpoints(this IEndpointRouteBuilder app) {
        app.MapAdminAccessRules();
        app.MapAdminAttachments();
        app.MapAdminCaseData();
        app.MapAdminCases();
        app.MapAdminCaseTypes();
        app.MapAdminCheckpointTypes();
        app.MapAdminIntegration();
        app.MapAdminNotifications();
        app.MapAdminQueries();
        app.MapAdminReports();
        app.MapAdminWorkflowInvoker();
        app.MapLookup();
    }
}
