using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>Manage Notifications for Back-office users.</summary>
public static class AdminNotificationsApi
{

    /// <summary>Maps admin notifications endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminNotifications(this IEndpointRouteBuilder routes) {

        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/my/notifications");
        
        group.WithTags("AdminNotifications");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
        );//.RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status400BadRequest);
        
        group.MapGet("", AdminNotificationsHandler.GetMySubscriptions)
            .WithName(nameof(AdminNotificationsHandler.GetMySubscriptions))
                .WithSummary("Get the notification subscriptions for a user.");
        
        group.MapPost("", AdminNotificationsHandler.Subscribe)
            .WithName(nameof(AdminNotificationsHandler.Subscribe))
                .WithSummary("Store user's subscription settings.");

        return group;
    }
}
