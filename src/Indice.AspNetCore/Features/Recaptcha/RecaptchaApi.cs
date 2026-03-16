using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Contains operations for managing a user's account.</summary>
public static class RecaptchaApi
{
    /// <summary>Adds Identity recaptcha endpoints.</summary>
    /// <param name="routes">Route builder.</param>
    public static IEndpointRouteBuilder MapRecaptcha(this IEndpointRouteBuilder routes) {
        var group = routes.MapGroup("/");
        group.WithTags("Recaptcha");
        group.WithGroupName("Recaptcha");
        group.ProducesProblem(StatusCodes.Status500InternalServerError);
        group.MapPost("RecaptchaValidate", RecaptchaHandlers.ValidateRecaptcha)
             .WithName(nameof(RecaptchaHandlers.ValidateRecaptcha))
             .WithSummary("Validates a recaptcha response.")
             .AllowAnonymous()
             .RequireRateLimiting("recaptcha");
        return group;
    }
}