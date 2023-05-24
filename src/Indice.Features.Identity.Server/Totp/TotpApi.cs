using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Totp;
using Indice.Features.Identity.Server.Totp.Models;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Contains operations for sending and validating TOTP messages for the current user.</summary>
public static class TotpApi
{
    /// <summary>Registers operations for sending and validating TOTP messages for the current user.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapTotps(this IdentityServerEndpointRouteBuilder builder) {
        var options = builder.GetEndpointOptions();
        var group = builder.MapGroup($"{options.ApiPrefix}/totp")
                           .WithGroupName("identity")
                           .WithTags("Totp")
                           .RequireAuthorization(policy => policy
                              .RequireAuthenticatedUser()
                              .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                           )
                           .ProducesProblem(StatusCodes.Status401Unauthorized)
                           .ProducesProblem(StatusCodes.Status403Forbidden)
                           .ProducesProblem(StatusCodes.Status500InternalServerError)
                           .RequireRateLimiting(IdentityEndpoints.RateLimiter.Policies.Totp);
        var allowedScopes = new[] { options.ApiScope }.Where(x => x != null).Cast<string>().ToArray();
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        // POST: /api/totp
        group.MapPost(string.Empty, TotpHandlers.Send)
             .WithName(nameof(TotpHandlers.Send))
             .WithSummary("Sends a new code via the selected channel.")
             .ProducesProblem(StatusCodes.Status405MethodNotAllowed)
             .WithParameterValidation<TotpRequest>();

        // PUT: /api/totp
        group.MapPut(string.Empty, TotpHandlers.Verify)
             .WithName(nameof(TotpHandlers.Verify))
             .WithSummary("Verify the code received.")
             .WithParameterValidation<TotpVerificationRequest>();

        return group;
    }
}
