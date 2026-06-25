using Indice.Features.Agents.Server;
using Indice.Features.Agents.Server.Endpoints;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>HTTP surface for the caller's application-local user profile: get, update preferences.</summary>
internal static class MyProfileApi
{
    /// <summary>Maps the <c>/api/my/profile</c> endpoint group.</summary>
    public static RouteGroupBuilder MapMyProfile(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<AgentsServerOptions>>().Value;
        var group = routes.MapGroup($"{options.PathPrefix.Value?.TrimEnd('/')}/my/profile")
                          .WithName(options.GroupName)
                          .WithTags("MyProfile");

        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser());
        group.WithOpenApiSecurityRequirement("oauth2");
        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet(string.Empty, MyProfileHandlers.GetMe)
             .WithName(nameof(MyProfileHandlers.GetMe))
             .WithSummary("Get the caller's profile.")
             .WithDescription("Returns the caller's application-local profile, creating it on first access (just-in-time) from the IdP claims. The IdP stays the source of truth for identity; this row holds app-specific preferences plus a cached snapshot of name/email/locale.");

        group.MapPut(string.Empty, MyProfileHandlers.UpdateMe)
             .WithParameterValidation<UpdateUserRequest>()
             .WithName(nameof(MyProfileHandlers.UpdateMe))
             .WithSummary("Update the caller's preferences.")
             .WithDescription("Updates the caller's app-specific preferences (preferred answer language, response style). The language is validated against the configured taxonomy — an unknown value returns 400.");

        return group;
    }
}
