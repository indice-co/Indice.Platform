using Indice.Features.Messages.AspNetCore.Endpoints;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Security;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides endpoints for managing campaign-related operations, specifically creating and publishing campaigns.
/// </summary>
internal static class SendApi
{
    /// <summary>Registers the endpoints for Send API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapSend(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<MessageManagementOptions>>().Value;
        var group = routes.MapGroup(options.PathPrefix.TrimEnd('/') + "/send");
        group.WithGroupName("send");
        group.WithTags("send");
        var allowedScopes = new[] { options.RequiredScope, options.SendRequiredScope }.Where(x => x != null).ToArray();

        group.RequireAuthorization(pb => pb.AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                                           .RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));

        group.AddOpenApiSecurityRequirement("oauth2", allowedScopes).WithOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost(string.Empty, SendHandlers.SendCampaign)
             .WithName(nameof(SendHandlers.SendCampaign))
             .WithSummary("Creates a new campaign.")
             .WithDescription(SendHandlers.CREATE_CAMPAIGN_DESCRIPTION)
             .WithParameterValidation<SendRequest>();

        return group;
    }
}