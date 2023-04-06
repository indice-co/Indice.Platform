using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Totp;
using Indice.Features.Identity.Server.Totp.Models;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Contains operations for sending and validating TOTP messages for the current user
/// </summary>
public static class TotpApi
{
    /// <summary>
    /// Register operations for sending and validating TOTP messages for the current user
    /// </summary>
    /// <param name="routes"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapTotps(this IdentityServerEndpointRouteBuilder routes) {
        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/totp");
        group.WithTags("Totp");
        group.WithGroupName("identity");
        // Add security requirements, all incoming requests to this API *must*
        // be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope }.Where(x => x != null).ToArray();
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme));


        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("", TotpHandlers.Send)
             .WithName(nameof(TotpHandlers.Send))
             .WithSummary("Sends a new code via the selected channel.")
             .ProducesProblem(StatusCodes.Status405MethodNotAllowed)
             .WithParameterValidation<TotpRequest>();

        group.MapPut("", TotpHandlers.Verify)
             .WithName(nameof(TotpHandlers.Verify))
             .WithSummary("Verify the code received.")
             .WithParameterValidation<TotpVerificationRequest>();

        return group;
    }
}
