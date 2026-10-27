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
             .WithName(nameof(PaymentsHandlers.StripeWebhook))
             .AllowAnonymous()
             .ExcludeFromDescription()
             .DisableAntiforgery();

        // Viva verifies the webhook URL with a GET before it starts posting events to it.
        group.MapGet("webhooks/viva", PaymentsHandlers.VivaWebhookVerification)
             .WithName(nameof(PaymentsHandlers.VivaWebhookVerification))
             .AllowAnonymous()
             .ExcludeFromDescription();

        group.MapPost("webhooks/viva", PaymentsHandlers.VivaWebhook)
             .WithName(nameof(PaymentsHandlers.VivaWebhook))
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

        chats.MapPost("retry", PaymentsHandlers.RetryPaymentAttempt)
             .WithName(nameof(PaymentsHandlers.RetryPaymentAttempt))
             .WithSummary("Replace a failed or cancelled payment attempt of a case.");

        chats.MapGet("viva/configuration", PaymentsHandlers.GetVivaClientConfiguration)
             .WithName(nameof(PaymentsHandlers.GetVivaClientConfiguration))
             .WithSummary("Get a fresh Viva Native Checkout configuration (access token and URLs) for the card form.");

        chats.MapPost("viva/charge", PaymentsHandlers.ChargeVivaPaymentAttempt)
             .WithName(nameof(PaymentsHandlers.ChargeVivaPaymentAttempt))
             .WithSummary("Charge the case's Viva payment attempt with a Native Checkout card token.");

        return routes;
    }
}
