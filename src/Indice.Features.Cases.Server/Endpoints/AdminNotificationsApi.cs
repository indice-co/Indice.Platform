using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Authorization;
using Indice.Features.Cases.Server.Endpoints;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Manage Notifications for Back-office users.</summary>
internal static class AdminNotificationsApi
{

    /// <summary>Maps admin notifications endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminNotifications(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/my/notifications");

        group.WithTags("AdminNotifications");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(pb => pb
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            .RequireCasesAccess(CasesAccessLevel.Manager)
        );
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet(string.Empty, AdminNotificationsHandler.GetMySubscriptions)
            .WithName(nameof(AdminNotificationsHandler.GetMySubscriptions))
            .WithSummary("Get the notification subscriptions for a user.");

        group.MapPost(string.Empty, AdminNotificationsHandler.Subscribe)
            .WithName(nameof(AdminNotificationsHandler.Subscribe))
            .WithSummary("Store user's subscription settings.");

        return group;
    }
}
