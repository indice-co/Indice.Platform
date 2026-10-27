using System.Security.Claims;
using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Payments;
using Indice.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Indice.Features.Agents.Server.Endpoints;

/// <summary>Logic-free handlers for the chat payments endpoints.</summary>
internal static class PaymentsHandlers
{
    /// <summary>GET /api/my/cases/{caseId}/payment-attempts/current — the case's latest payment attempt.</summary>
    public static async Task<Results<Ok<PaymentSessionModel>, NotFound>> GetCurrentPaymentAttempt(string caseId, ClaimsPrincipal user, IPaymentAttemptsService payments, CancellationToken cancellationToken) {
        var session = await payments.GetCurrentAttemptAsync(user.FindSubjectId()!, caseId, AgentsConstants.DefaultTenantId, cancellationToken);
        return session is null ? TypedResults.NotFound() : TypedResults.Ok(session);
    }

    /// <summary>POST /api/my/cases/{caseId}/payment-attempts/retry — replaces the caller's failed or cancelled attempt (or returns the live one).</summary>
    public static async Task<Results<Ok<PaymentSessionModel>, NotFound>> RetryPaymentAttempt(string caseId, ClaimsPrincipal user, IPaymentAttemptsService payments, CancellationToken cancellationToken) {
        var session = await payments.RetryAttemptAsync(user.FindSubjectId()!, caseId, AgentsConstants.DefaultTenantId, cancellationToken);
        return session is null ? TypedResults.NotFound() : TypedResults.Ok(session);
    }

    /// <summary>GET /api/my/cases/{caseId}/payment-attempts/viva/configuration — a fresh Native Checkout configuration.</summary>
    public static async Task<Results<Ok<VivaClientConfiguration>, NotFound>> GetVivaClientConfiguration(string caseId, IPaymentAttemptsService payments, CancellationToken cancellationToken) {
        if (payments.GetProvider(PaymentProviders.Viva) is not IVivaPaymentsService viva) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(await viva.GetClientConfigurationAsync(cancellationToken));
    }

    /// <summary>POST /api/my/cases/{caseId}/payment-attempts/viva/charge — charges the caller's Viva attempt with a card token.</summary>
    public static async Task<Results<Ok<PaymentSessionModel>, NotFound, ValidationProblem>> ChargeVivaPaymentAttempt(string caseId, VivaChargeRequestModel request, ClaimsPrincipal user, IPaymentAttemptsService payments, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(request?.ChargeToken)) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [nameof(VivaChargeRequestModel.ChargeToken)] = ["The charge token is required."] });
        }
        if (payments.GetProvider(PaymentProviders.Viva) is not IVivaPaymentsService viva) {
            return TypedResults.NotFound();
        }
        var session = await viva.ChargeAsync(user.FindSubjectId()!, caseId, AgentsConstants.DefaultTenantId, request.ChargeToken, cancellationToken);
        return session is null ? TypedResults.NotFound() : TypedResults.Ok(session);
    }

    /// <summary>GET /api/webhooks/viva — answers Viva's webhook URL verification with the merchant key.</summary>
    public static async Task<Results<Ok<VivaWebhookKey>, NotFound>> VivaWebhookVerification(IPaymentAttemptsService payments, CancellationToken cancellationToken) {
        if (payments.GetProvider(PaymentProviders.Viva) is not IVivaPaymentsService viva) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(new VivaWebhookKey(await viva.GetWebhookKeyAsync(cancellationToken)));
    }

    /// <summary>POST /api/webhooks/viva — Viva webhook receiver (each event is re-verified against the Viva API).</summary>
    public static async Task<Results<Ok, NotFound>> VivaWebhook(HttpRequest request, IPaymentAttemptsService payments, CancellationToken cancellationToken) {
        if (payments.GetProvider(PaymentProviders.Viva) is not IVivaPaymentsService viva) {
            return TypedResults.NotFound();
        }
        using var reader = new StreamReader(request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);
        await viva.HandleWebhookAsync(json, cancellationToken);
        return TypedResults.Ok();
    }

    /// <summary>POST /api/webhooks/stripe — Stripe webhook receiver (signature verified).</summary>
    public static async Task<Results<Ok, BadRequest>> StripeWebhook(HttpRequest request, IPaymentsService payments, ILoggerFactory loggerFactory, CancellationToken cancellationToken) {
        using var reader = new StreamReader(request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);
        try {
            await payments.HandleWebhookAsync(json, request.Headers["Stripe-Signature"], cancellationToken);
        } catch (StripeException ex) {
            loggerFactory.CreateLogger(typeof(PaymentsHandlers)).LogWarning(ex, "Rejected Stripe webhook.");
            return TypedResults.BadRequest();
        }
        return TypedResults.Ok();
    }
}
