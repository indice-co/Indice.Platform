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
    public static async Task<Results<Ok<PaymentSessionModel>, NotFound>> GetCurrentPaymentAttempt(string caseId, ClaimsPrincipal user, IPaymentsService payments, CancellationToken cancellationToken) {
        var session = await payments.GetCurrentAttemptAsync(user.FindSubjectId()!, caseId, AgentsConstants.DefaultTenantId, cancellationToken);
        return session is null ? TypedResults.NotFound() : TypedResults.Ok(session);
    }

    /// <summary>POST /api/my/cases/{caseId}/payment-attempts/retry — replaces the caller's failed or cancelled attempt (or returns the live one).</summary>
    public static async Task<Results<Ok<PaymentSessionModel>, NotFound>> RetryPaymentAttempt(string caseId, ClaimsPrincipal user, IPaymentsService payments, CancellationToken cancellationToken) {
        var session = await payments.RetryAttemptAsync(user.FindSubjectId()!, caseId, AgentsConstants.DefaultTenantId, cancellationToken);
        return session is null ? TypedResults.NotFound() : TypedResults.Ok(session);
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
