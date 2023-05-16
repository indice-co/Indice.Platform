using Indice.AspNetCore.Http.Filters;
using Indice.Features.Identity.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Indice.Features.Identity.Server.Mfa;

/// <summary>MFA API.</summary>
public static class MfaApi
{
    /// <summary>Map MFA login endpoints.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static IEndpointRouteBuilder MapMultiFactorAuthentication(this IEndpointRouteBuilder builder) {
        var group = builder
            .MapGroup("")
            .WithGroupName("identity")
            .WithTags("Mfa")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // POST: /login/mfa/notify
        group.MapPost("login/mfa/notify", MfaApiHandlers.SendPushNotification)
             .WithName("MfaSendPushNotification")
             .WithSummary("Sends a push notification to the user's trusted mobile device for login approval.")
             .RequireAuthorization(policy => policy
                 .RequireAuthenticatedUser()
                 .AddAuthenticationSchemes(ExtendedIdentityConstants.TwoFactorUserIdScheme)
             )
             .ValidateAntiforgery();

        // POST: /login/mfa/approve
        group.MapPost("login/mfa/approve", MfaApiHandlers.ApproveMfaLogin)
             .WithName("MfaApproveLogin")
             .WithSummary("Must be called when the MFA login was approved by the client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeDeviceAuthenticated);

        // POST: /login/mfa/reject
        group.MapPost("login/mfa/reject", MfaApiHandlers.RejectMfaLogin)
             .WithName("MfaRejectLogin")
             .WithSummary("Must be called when the MFA login was rejected by the client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeDeviceAuthenticated);

        return builder;
    }
}
