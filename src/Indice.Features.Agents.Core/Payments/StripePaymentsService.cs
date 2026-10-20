using Indice.Features.Agents.Core.Data;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace Indice.Features.Agents.Core.Payments;

/// <summary>Chat payments: Stripe one-step payment (the full amount is charged on confirmation).</summary>
public interface IPaymentsService
{
    /// <summary>The publishable key for the browser.</summary>
    PaymentProviderPublicKey GetPublicKey();
    /// <summary>Creates a PaymentIntent for the case, or returns the live attempt when one exists (idempotent). A case is paid once.</summary>
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
    /// <summary>Cancels the case's unpaid attempt.</summary>
    Task<PaymentSessionModel?> CancelAsync(string caseId, CancellationToken cancellationToken = default);
}

/// <summary>Stripe implementation of <see cref="IPaymentsService"/>.</summary>
public class StripePaymentsService(
    AgentsDbContext db,
    IStripeClient stripeClient,
    IOptions<StripeOptions> options,
    IPaymentEventHandler eventHandler,
    TimeProvider timeProvider,
    ILogger<StripePaymentsService> logger) : IPaymentsService
{
    private readonly StripeOptions _options = options.Value;
    private PaymentIntentService PaymentIntents => new(stripeClient);
    ///<inheritdoc/>
    public PaymentProviderPublicKey GetPublicKey() => new() { PublishableKey = _options.PublishableKey, PaymentMethodOrder = _options.PaymentMethodOrder };
    ///<inheritdoc/>
    public async Task<PaymentSessionModel?> CreateOrReuseAttemptAsync(string userId, string caseId, decimal amount, Guid conversationId, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        var attempts = db.PaymentSessions.Where(x => x.CaseId == caseId);
        var live = await attempts.Where(x => x.Status == PaymentAuthorizationStatus.Created || x.Status == PaymentAuthorizationStatus.Succeeded)
                                 .OrderByDescending(x => x.CreatedAt)
                                 .FirstOrDefaultAsync(cancellationToken);
        if (live is not null) {
            await SyncWithStripeAsync(live, cancellationToken);
        }
        if (live is not null && live.Status is PaymentAuthorizationStatus.Created or PaymentAuthorizationStatus.Succeeded) {
            return ToModel(live);
        }
        var attemptNumber = await attempts.CountAsync(cancellationToken) + 1;
        var paymentIntent = await PaymentIntents.CreateAsync(new PaymentIntentCreateOptions {
            Amount = ToMinorUnits(amount),
            Currency = _options.Currency,
            // Methods
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true, AllowRedirects = "never" },
            Metadata = new Dictionary<string, string> {
                ["caseId"] = caseId,
                ["conversationId"] = conversationId.ToString(),
                ["userId"] = userId
            }
        }, new RequestOptions { IdempotencyKey = $"dex-{DateTime.Now.Ticks}-attempts:{caseId}:{attemptNumber}" }, cancellationToken);

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
            Amount = amount,
            Currency = _options.Currency,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.PaymentSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return ToModel(session);
    }
    ///<inheritdoc/>
    public async Task<PaymentSessionModel?> GetCurrentAttemptAsync(string userId, string caseId, CancellationToken cancellationToken = default) {
        var session = await db.PaymentSessions.Where(x => x.CaseId == caseId && x.UserId == userId)
                                              .OrderByDescending(x => x.CreatedAt)
                                              .FirstOrDefaultAsync(cancellationToken);
        if (session is not null) {
            await SyncWithStripeAsync(session, cancellationToken);
        }
        return session is null ? null : ToModel(session);
    }
    // The webhook is the primary source of truth but can be late or missing (e.g. no forwarding in development).
    // An attempt still marked Created is reconciled against its PaymentIntent so a paid intent is never re-confirmed.
    private async Task SyncWithStripeAsync(DbPaymentSession session, CancellationToken cancellationToken) {
        if (session.Status != PaymentAuthorizationStatus.Created) {
            return;
        }
        var intent = await PaymentIntents.GetAsync(session.PaymentIntentId, cancellationToken: cancellationToken);
        PaymentAuthorizationStatus? status = intent.Status switch {
            "succeeded" => PaymentAuthorizationStatus.Succeeded,
            "canceled" => PaymentAuthorizationStatus.Cancelled,
            _ => null
        };
        if (status is null) {
            return;
        }
        session.Status = status.Value;
        session.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        await eventHandler.OnStatusChangedAsync(ToModel(session), cancellationToken);
    }
    ///<inheritdoc/>
    public async Task HandleWebhookAsync(string json, string? signatureHeader, CancellationToken cancellationToken = default) {
        var stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _options.WebhookSecret, throwOnApiVersionMismatch: false);
        if (stripeEvent.Data.Object is not PaymentIntent paymentIntent) {
            return;
        }
        PaymentAuthorizationStatus? status = stripeEvent.Type switch {
            EventTypes.PaymentIntentSucceeded => PaymentAuthorizationStatus.Succeeded,
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
        session.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        await eventHandler.OnStatusChangedAsync(ToModel(session), cancellationToken);
    }
    ///<inheritdoc/>
    public async Task<PaymentSessionModel?> CancelAsync(string caseId, CancellationToken cancellationToken = default) {
        var session = await db.PaymentSessions.Where(x => x.CaseId == caseId && x.Status == PaymentAuthorizationStatus.Created)
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
        Amount = session.Amount,
        Currency = session.Currency
    };
}
