using Indice.Features.Agents.Server;
using Indice.Features.Agents.Server.Endpoints;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>HTTP surface for chat payments: Stripe one-step payment.</summary>
internal static class PaymentsApi
{
    /// <summary>Maps the payment endpoints.</summary>
    public static IEndpointRouteBuilder MapPayments(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<AgentsServerOptions>>().Value;
        var allowedScopes = new[] { options.ChatRequiredScope }.FilterOutNulls().ToArray();
        var prefix = options.PathPrefix.Value?.TrimEnd('/');

        var group = routes.MapGroup($"{prefix}/")
                          .WithName(options.GroupName)
                          .WithTags("Payments");
        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("webhooks/stripe", PaymentsHandlers.StripeWebhook)
             .AllowAnonymous()
             .ExcludeFromDescription()
             .DisableAntiforgery();

        var chats = group.MapGroup("my/cases/{caseId}/payment-attempts");
        chats.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));
        chats.WithOpenApiSecurityRequirement("oauth2", allowedScopes);
        chats.ProducesProblem(StatusCodes.Status401Unauthorized);

        chats.MapGet("current", PaymentsHandlers.GetCurrentPaymentAttempt)
             .WithName(nameof(PaymentsHandlers.GetCurrentPaymentAttempt))
             .WithSummary("Get the latest payment attempt of a case.");

        return routes;
    }
}
