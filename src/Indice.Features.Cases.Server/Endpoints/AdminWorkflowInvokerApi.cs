using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>Invoking workflow activities for suspended instances.</summary>
internal static class AdminWorkflowInvokerApi
{
    /// <summary>Maps admin workflow invoker endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminWorkflowInvoker(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage");
        group.WithTags("AdminWorkflowInvoker");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();

        group.RequireAuthorization(pb => pb
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
            .RequireCasesAccess(Authorization.CasesAccessLevel.Manager)
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
        ).RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("cases/{caseId:guid}/approve", AdminWorkflowInvokerHandler.SubmitApproval)
            .WithName(nameof(AdminWorkflowInvokerHandler.SubmitApproval))
            .WithSummary("Invoke the approval activity to approve or reject the case.");

        group.MapPost("cases/{caseId:guid}/assign", AdminWorkflowInvokerHandler.AssignCase)
            .WithName(nameof(AdminWorkflowInvokerHandler.AssignCase))
            .WithSummary("Invoke the assign activity to assign the case to the caller user.");

        group.MapPost("cases/{caseId:guid}/edit", AdminWorkflowInvokerHandler.EditCase)
            .WithName(nameof(AdminWorkflowInvokerHandler.EditCase))
            .WithSummary("Invoke the edit activity to edit the data of the case.");

        group.MapPost("cases/{caseId:guid}/trigger-action", AdminWorkflowInvokerHandler.TriggerAction)
            .WithName(nameof(AdminWorkflowInvokerHandler.TriggerAction))
            .WithSummary("Invoke the action activity to trigger a business action for the case.");

        return group;
    }
}
