using Indice.Features.Agents.Core.Data;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace Indice.Features.Agents.Core.Payments;

/// <summary>Chat payments: Stripe card pre-authorization with manual capture.</summary>
public interface IPaymentsService
{
    /// <summary>The publishable key for the browser.</summary>
    PaymentProviderPublicKey GetPublicKey();
    /// <summary>Creates a manual-capture PaymentIntent for the case, or returns the live attempt when one exists (idempotent). A case is paid once.</summary>
    /// <param name="userId">The paying user.</param>
    /// <param name="caseId">The case the payment is for.</param>
    /// <param name="amount">The amount to be charged.</param>
    /// <param name="conversationId">The chat the payment is started from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PaymentSessionModel?> CreateOrReuseAttemptAsync(string userId, string caseId, decimal amount, Guid conversationId, CancellationToken cancellationToken = default);
    /// <summary>The latest payment attempt of the case.</summary>
    Task<PaymentSessionModel?> GetCurrentAttemptAsync(string userId, string caseId, CancellationToken cancellationToken = default);
    /// <summary>Verifies and applies a Stripe webhook event.</summary>
    /// <exception cref="StripeException">The signature is invalid.</exception>
    Task HandleWebhookAsync(string json, string? signatureHeader, CancellationToken cancellationToken = default);
    /// <summary>Captures the final amount (capped to the hold) of the case's authorized attempt. A zero amount cancels and releases the hold.</summary>
    Task<PaymentSessionModel?> CaptureAsync(string caseId, decimal amount, CancellationToken cancellationToken = default);
    /// <summary>Cancels the case's live attempt and releases the hold.</summary>
    Task<PaymentSessionModel?> CancelAsync(string caseId, CancellationToken cancellationToken = default);
}

/// <summary>Hook for the business flow that runs once a payment is authorized (or fails/cancels). Called from the webhook.</summary>
public interface IPaymentEventHandler
{
    /// <summary>Called when the status of a payment attempt changes.</summary>
    Task OnStatusChangedAsync(PaymentSessionModel session, CancellationToken cancellationToken = default);
}

internal sealed class NoOpPaymentEventHandler : IPaymentEventHandler
{
    public Task OnStatusChangedAsync(PaymentSessionModel session, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class StripePaymentsService(
    AgentsDbContext db,
    IStripeClient stripeClient,
    IOptions<StripeOptions> options,
    IPaymentEventHandler eventHandler,
    TimeProvider timeProvider,
    ILogger<StripePaymentsService> logger) : IPaymentsService
{
    private readonly StripeOptions _options = options.Value;
    private PaymentIntentService PaymentIntents => new(stripeClient);

    public PaymentProviderPublicKey GetPublicKey() => new() { PublishableKey = _options.PublishableKey };

    public async Task<PaymentSessionModel?> CreateOrReuseAttemptAsync(string userId, string caseId, decimal amount, Guid conversationId, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        var attempts = db.PaymentSessions.Where(x => x.CaseId == caseId);
        var live = await attempts.Where(x => x.Status == PaymentAuthorizationStatus.Created || x.Status == PaymentAuthorizationStatus.Succeeded)
                                 .OrderByDescending(x => x.CreatedAt)
                                 .FirstOrDefaultAsync(cancellationToken);
        if (live is not null) {
            return ToModel(live);
        }
        var attemptNumber = await attempts.CountAsync(cancellationToken) + 1;
        var paymentIntent = await PaymentIntents.CreateAsync(new PaymentIntentCreateOptions {
            Amount = ToMinorUnits(amount),
            Currency = _options.Currency,
            CaptureMethod = "manual",
            PaymentMethodTypes = ["card"],
            Metadata = new Dictionary<string, string> {
                ["caseId"] = caseId,
                ["conversationId"] = conversationId.ToString(),
                ["userId"] = userId
            }
        }, new RequestOptions { IdempotencyKey = $"dex-payment-attempts:{caseId}:{attemptNumber}" }, cancellationToken);

        // The idempotency key may hand back a PaymentIntent already stored by a concurrent request.
        var existing = await db.PaymentSessions.FirstOrDefaultAsync(x => x.PaymentIntentId == paymentIntent.Id, cancellationToken);
        if (existing is not null) {
            return ToModel(existing);
        }
        var now = timeProvider.GetUtcNow();
        var session = new DbPaymentSession {
            CaseId = caseId,
            ConversationId = conversationId,
            UserId = userId,
            PaymentIntentId = paymentIntent.Id,
            ClientSecret = paymentIntent.ClientSecret,
            Status = PaymentAuthorizationStatus.Created,
            FirstCaptureAmount = amount,
            Currency = _options.Currency,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.PaymentSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return ToModel(session);
    }

    public async Task<PaymentSessionModel?> GetCurrentAttemptAsync(string userId, string caseId, CancellationToken cancellationToken = default) {
        var session = await db.PaymentSessions.AsNoTracking()
                                              .Where(x => x.CaseId == caseId && x.UserId == userId)
                                              .OrderByDescending(x => x.CreatedAt)
                                              .FirstOrDefaultAsync(cancellationToken);
        return session is null ? null : ToModel(session);
    }

    public async Task HandleWebhookAsync(string json, string? signatureHeader, CancellationToken cancellationToken = default) {
        var stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _options.WebhookSecret, throwOnApiVersionMismatch: false);
        if (stripeEvent.Data.Object is not PaymentIntent paymentIntent) {
            return;
        }
        PaymentAuthorizationStatus? status = stripeEvent.Type switch {
            EventTypes.PaymentIntentAmountCapturableUpdated or EventTypes.PaymentIntentSucceeded => PaymentAuthorizationStatus.Succeeded,
            EventTypes.PaymentIntentPaymentFailed => PaymentAuthorizationStatus.Failed,
            EventTypes.PaymentIntentCanceled => PaymentAuthorizationStatus.Cancelled,
            _ => null
        };
        if (status is null) {
            return;
        }
        var session = await db.PaymentSessions.FirstOrDefaultAsync(x => x.PaymentIntentId == paymentIntent.Id, cancellationToken);
        if (session is null) {
            logger.LogWarning("Stripe event {EventType} for unknown PaymentIntent {PaymentIntentId}.", stripeEvent.Type, paymentIntent.Id);
            return;
        }
        if (session.Status == status) {
            return; // Stripe retries and duplicates events.
        }
        session.Status = status.Value;
        session.FailureReason = paymentIntent.LastPaymentError?.Message;
        if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded && paymentIntent.AmountReceived > 0) {
            session.CapturedAmount = paymentIntent.AmountReceived / 100m;
        }
        session.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        await eventHandler.OnStatusChangedAsync(ToModel(session), cancellationToken);
    }

    public async Task<PaymentSessionModel?> CaptureAsync(string caseId, decimal amount, CancellationToken cancellationToken = default) {
        if (amount <= 0) {
            return await CancelAsync(caseId, cancellationToken);
        }
        var session = await db.PaymentSessions.Where(x => x.CaseId == caseId && x.Status == PaymentAuthorizationStatus.Succeeded && x.CapturedAmount == null)
                                              .OrderByDescending(x => x.CreatedAt)
                                              .FirstOrDefaultAsync(cancellationToken);
        if (session is null) {
            return null;
        }
        var amountToCapture = Math.Min(amount, session.FirstCaptureAmount);
        var paymentIntent = await PaymentIntents.CaptureAsync(session.PaymentIntentId, new PaymentIntentCaptureOptions {
            AmountToCapture = ToMinorUnits(amountToCapture)
        }, new RequestOptions { IdempotencyKey = $"dex-payment-capture:{session.PaymentIntentId}" }, cancellationToken);
        session.CapturedAmount = paymentIntent.AmountReceived / 100m;
        session.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return ToModel(session);
    }

    public async Task<PaymentSessionModel?> CancelAsync(string caseId, CancellationToken cancellationToken = default) {
        var session = await db.PaymentSessions.Where(x => x.CaseId == caseId && x.CapturedAmount == null
                                                       && (x.Status == PaymentAuthorizationStatus.Created || x.Status == PaymentAuthorizationStatus.Succeeded))
                                              .OrderByDescending(x => x.CreatedAt)
                                              .FirstOrDefaultAsync(cancellationToken);
        if (session is null) {
            return null;
        }
        try {
            await PaymentIntents.CancelAsync(session.PaymentIntentId, cancellationToken: cancellationToken);
        } catch (StripeException ex) {
            throw new BusinessException($"The payment could not be cancelled: {ex.StripeError?.Message ?? ex.Message}");
        }
        session.Status = PaymentAuthorizationStatus.Cancelled;
        session.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return ToModel(session);
    }

    private static long ToMinorUnits(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    private static PaymentSessionModel ToModel(DbPaymentSession session) => new() {
        Id = session.Id,
        CaseId = session.CaseId,
        ConversationId = session.ConversationId,
        // Only a confirmable attempt needs its secret on the wire.
        PaymentSessionSecret = session.Status == PaymentAuthorizationStatus.Created ? session.ClientSecret : null,
        PaymentAuthorizationStatus = session.Status,
        FirstCaptureAmount = session.FirstCaptureAmount,
        CapturedAmount = session.CapturedAmount,
        Currency = session.Currency
    };
}
