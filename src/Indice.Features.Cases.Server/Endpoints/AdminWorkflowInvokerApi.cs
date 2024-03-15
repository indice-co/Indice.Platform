using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Cases.Server.Options;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>Invoking workflow activities for suspended instances.</summary>
public static class AdminWorkflowInvokerApi
{
    /// <summary>Maps admin workflow invoker endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminWorkflowInvoker(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerEndpointOptions>>().Value;
        var group = routes.MapGroup($"{options.ApiPrefix}/manage");
        group.WithTags("AdminWorkflowInvoker");
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        group.WithGroupName(ApiGroups.CasesApiGroupNamePlaceholder);
        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(CasesApiConstants.AuthenticationScheme)
            .RequireClaim(BasicClaimTypes.Scope, options.ApiScope)
        ).RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", options.ApiScope);
        group.MapPost("cases/{caseId}/approve", AdminWorkflowInvokerHandler.SubmitApproval)
            .WithName(nameof(AdminWorkflowInvokerHandler.SubmitApproval))
                .WithSummary("Invoke the approval activity to approve or reject the case.");
        group.MapPost("cases/{caseId}/assign", AdminWorkflowInvokerHandler.AssignCase)
            .WithName(nameof(AdminWorkflowInvokerHandler.AssignCase))
                .WithSummary("Invoke the assign activity to assign the case to the caller user.");
        group.MapPost("cases/{caseId}/edit", AdminWorkflowInvokerHandler.EditCase)
            .WithName(nameof(AdminWorkflowInvokerHandler.EditCase))
                .WithSummary("Invoke the edit activity to edit the data of the case.");
        group.MapPost("cases/{caseId}/trigger-action", AdminWorkflowInvokerHandler.TriggerAction)
            .WithName(nameof(AdminWorkflowInvokerHandler.TriggerAction))
                .WithSummary("Invoke the action activity to trigger a business action for the case.");
        return group;
    }
}
