using System.Text.Json;
using Indice.Features.Agents.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Payments;

/// <summary>What the browser needs to render the Viva Native Checkout card form.</summary>
/// <param name="AuthToken">The short-lived access token passed to <c>VivaPayments.cards.setup</c>.</param>
/// <param name="BaseUrl">The API URL passed as <c>baseURL</c>.</param>
/// <param name="ScriptUrl">The Native Checkout v2 script URL.</param>
/// <param name="ExpiresAt">When <paramref name="AuthToken"/> expires.</param>
public record VivaClientConfiguration(string AuthToken, string BaseUrl, string ScriptUrl, DateTimeOffset ExpiresAt);

/// <summary>Chat payments with Viva Wallet Native Checkout v2 (direct charge, the card form is rendered in place).</summary>
public interface IVivaPaymentsService : IPaymentProvider
{
    /// <summary>A fresh client configuration (access token, URLs) for the card form.</summary>
    Task<VivaClientConfiguration> GetClientConfigurationAsync(CancellationToken cancellationToken = default);
    /// <summary>Charges the case's live Viva attempt with the card token produced in the browser.</summary>
    /// <param name="userId">The paying user; must own the attempt.</param>
    /// <param name="caseId">The case being paid.</param>
    /// <param name="tenantId">The tenant the case belongs to.</param>
    /// <param name="chargeToken">The token returned by <c>VivaPayments.cards.requestToken</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The attempt after the charge; <c>null</c> when the user has no attempt for the case.</returns>
    Task<PaymentSessionModel?> ChargeAsync(string userId, string caseId, string tenantId, string chargeToken, CancellationToken cancellationToken = default);
    /// <summary>The key returned to Viva when it verifies the webhook URL.</summary>
    Task<string> GetWebhookKeyAsync(CancellationToken cancellationToken = default);
    /// <summary>Handles a Viva webhook notification. The event is never trusted: the transaction is re-read from the API.</summary>
    Task HandleWebhookAsync(string json, CancellationToken cancellationToken = default);
}

/// <summary>Viva Wallet implementation of <see cref="IVivaPaymentsService"/>.</summary>
public class VivaPaymentsService(
    AgentsDbContext db,
    IVivaClient vivaClient,
    IOptions<VivaOptions> options,
    IPaymentEventHandler eventHandler,
    ILogger<VivaPaymentsService> logger) : IVivaPaymentsService
{
    /// <summary>Transaction Payment Created.</summary>
    public const int EventTransactionPaymentCreated = 1796;
    /// <summary>Transaction Reversal Created.</summary>
    public const int EventTransactionReversalCreated = 1797;
    /// <summary>Transaction Failed.</summary>
    public const int EventTransactionFailed = 1798;
    private const string PendingPrefix = "viva:";
    private const string ChargingPrefix = "viva-charging:";
    private readonly VivaOptions _options = options.Value;

    ///<inheritdoc/>
    public string Name => PaymentProviders.Viva;

    ///<inheritdoc/>
    public async Task<VivaClientConfiguration> GetClientConfigurationAsync(CancellationToken cancellationToken = default) {
        var token = await vivaClient.GetAccessTokenAsync(cancellationToken);
        return new VivaClientConfiguration(token.Token, _options.GetApiUrl(), _options.GetScriptUrl(), token.ExpiresAt);
    }

    ///<inheritdoc/>
    public async Task<PaymentSessionModel?> CreateOrReuseAttemptAsync(string userId, string caseId, string tenantId, decimal amount, Guid conversationId, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        var attempts = db.PaymentSessions.Where(x => x.CaseId == caseId && x.TenantId == tenantId);
        // A paid case stays paid whatever the provider; only a Viva attempt can be resumed here.
        var live = await attempts.Where(x => x.Status == PaymentAuthorizationStatus.Succeeded || (x.Status == PaymentAuthorizationStatus.Created && x.Provider == PaymentProviders.Viva))
                                 .OrderByDescending(x => x.CreatedAt)
                                 .FirstOrDefaultAsync(cancellationToken);
        if (live is not null) {
            await SyncWithVivaAsync(live, cancellationToken);
            if (live.Status is PaymentAuthorizationStatus.Created or PaymentAuthorizationStatus.Succeeded) {
                return live.ToModel();
            }
        }
        var now = DateTimeOffset.UtcNow;
        var session = new DbPaymentSession {
            CaseId = caseId,
            TenantId = tenantId,
            ConversationId = conversationId,
            UserId = userId,
            Provider = PaymentProviders.Viva,
            Status = PaymentAuthorizationStatus.Created,
            Amount = amount,
            Currency = _options.Currency.ToLowerInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };
        // No order exists before the charge: the placeholder keeps the unique reference index satisfied.
        session.PaymentIntentId = $"{PendingPrefix}{session.Id:N}";
        db.PaymentSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return session.ToModel();
    }

    ///<inheritdoc/>
    public async Task<PaymentSessionModel?> ChargeAsync(string userId, string caseId, string tenantId, string chargeToken, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(chargeToken);
        var session = await db.PaymentSessions.Where(x => x.CaseId == caseId && x.TenantId == tenantId)
                                              .OrderByDescending(x => x.CreatedAt)
                                              .FirstOrDefaultAsync(cancellationToken);
        if (session is null || session.UserId != userId) {
            return null;
        }
        if (session.Provider != PaymentProviders.Viva || session.Status != PaymentAuthorizationStatus.Created) {
            return session.ToModel();
        }
        var pending = $"{PendingPrefix}{session.Id:N}";
        var charging = $"{ChargingPrefix}{session.Id:N}";
        // Claim the attempt so a double submit cannot charge the card twice.
        var claimed = await db.PaymentSessions.Where(x => x.Id == session.Id && x.Status == PaymentAuthorizationStatus.Created && x.PaymentIntentId == pending)
                                              .ExecuteUpdateAsync(s => s.SetProperty(x => x.PaymentIntentId, charging)
                                                                        .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
        await db.Entry(session).ReloadAsync(cancellationToken);
        if (claimed == 0) {
            return session.ToModel();
        }
        string transactionId;
        try {
            transactionId = await vivaClient.ChargeAsync(new VivaChargeRequest {
                Amount = ToMinorUnits(session.Amount),
                Preauth = false,
                SourceCode = _options.SourceCode!,
                ChargeToken = chargeToken,
                CurrencyCode = _options.GetCurrencyCode(),
                MerchantTrns = session.Id.ToString(),
                CustomerTrns = $"Case {session.CaseId}"
            }, cancellationToken);
        } catch (VivaException ex) when (ex.StatusCode is >= 400 and < 500) {
            logger.LogWarning(ex, "Viva rejected the charge of payment {PaymentId}.", session.Id);
            await TryTransitionAsync(session, PaymentAuthorizationStatus.Failed, "The card payment was declined.", cancellationToken);
            return session.ToModel();
        }
        await db.PaymentSessions.Where(x => x.Id == session.Id)
                                .ExecuteUpdateAsync(s => s.SetProperty(x => x.PaymentIntentId, transactionId)
                                                          .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
        await db.Entry(session).ReloadAsync(cancellationToken);
        await SyncWithVivaAsync(session, cancellationToken, force: true);
        return session.ToModel();
    }

    ///<inheritdoc/>
    public Task<string> GetWebhookKeyAsync(CancellationToken cancellationToken = default) => vivaClient.GetWebhookKeyAsync(cancellationToken);

    ///<inheritdoc/>
    public async Task HandleWebhookAsync(string json, CancellationToken cancellationToken = default) {
        if (!TryReadEvent(json, out var eventTypeId, out var transactionId, out var merchantTrns)) {
            logger.LogWarning("Ignored a malformed Viva webhook.");
            return;
        }
        if (eventTypeId is not (EventTransactionPaymentCreated or EventTransactionFailed or EventTransactionReversalCreated)) {
            return;
        }
        // Viva does not sign webhooks: trust only what the API reports for the transaction.
        var transaction = await vivaClient.GetTransactionAsync(transactionId!, cancellationToken);
        if (transaction is null) {
            logger.LogWarning("Viva event {EventTypeId} for unknown transaction {TransactionId}.", eventTypeId, transactionId);
            return;
        }
        var session = await db.PaymentSessions.FirstOrDefaultAsync(x => x.PaymentIntentId == transactionId, cancellationToken);
        if (session is null && Guid.TryParse(transaction.MerchantTrns ?? merchantTrns, out var paymentId)) {
            // The webhook can land before the charge response stored the transaction id.
            session = await db.PaymentSessions.FirstOrDefaultAsync(x => x.Id == paymentId && x.Provider == PaymentProviders.Viva, cancellationToken);
            if (session is not null && session.PaymentIntentId.StartsWith(ChargingPrefix, StringComparison.Ordinal)) {
                await db.PaymentSessions.Where(x => x.Id == session.Id && x.PaymentIntentId == session.PaymentIntentId)
                                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.PaymentIntentId, transactionId), cancellationToken);
                await db.Entry(session).ReloadAsync(cancellationToken);
            }
        }
        if (session is null) {
            logger.LogWarning("Viva event {EventTypeId} for transaction {TransactionId} matches no payment.", eventTypeId, transactionId);
            return;
        }
        if (session.Status != PaymentAuthorizationStatus.Created) {
            if (eventTypeId == EventTransactionReversalCreated) {
                logger.LogWarning("Viva reversal {TransactionId} received for payment {PaymentId} in status {Status}.", transactionId, session.Id, session.Status);
            }
            return; // Already finalized (duplicates/retries, or another writer won).
        }
        var status = MapStatus(transaction);
        if (status is null) {
            return;
        }
        await TryTransitionAsync(session, status.Value, status == PaymentAuthorizationStatus.Failed ? "The card payment was declined." : null, cancellationToken);
    }

    // Reconciles a Created attempt that already has a Viva transaction (charge response or webhook stored it).
    private async Task SyncWithVivaAsync(DbPaymentSession session, CancellationToken cancellationToken, bool force = false) {
        if (session.Status != PaymentAuthorizationStatus.Created || session.Provider != PaymentProviders.Viva || !HasTransaction(session)) {
            return;
        }
        var now = DateTimeOffset.UtcNow;
        if (!force && session.UpdatedAt > session.CreatedAt && now - session.UpdatedAt < _options.ReconciliationInterval) {
            return;
        }
        var transaction = await vivaClient.GetTransactionAsync(session.PaymentIntentId, cancellationToken);
        var status = transaction is null ? null : MapStatus(transaction);
        if (status is null) {
            await db.PaymentSessions.Where(x => x.Id == session.Id && x.Status == PaymentAuthorizationStatus.Created)
                                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.UpdatedAt, now), cancellationToken);
            await db.Entry(session).ReloadAsync(cancellationToken);
            return;
        }
        await TryTransitionAsync(session, status.Value, status == PaymentAuthorizationStatus.Failed ? "The card payment was declined." : null, cancellationToken);
    }

    // Atomic compare-and-set from Created; only the winning writer dispatches the status notification.
    private async Task<bool> TryTransitionAsync(DbPaymentSession session, PaymentAuthorizationStatus status, string? failureReason, CancellationToken cancellationToken) {
        var now = DateTimeOffset.UtcNow;
        var affected = await db.PaymentSessions
            .Where(x => x.Id == session.Id && x.Status == PaymentAuthorizationStatus.Created)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status)
                                      .SetProperty(x => x.FailureReason, failureReason)
                                      .SetProperty(x => x.UpdatedAt, now), cancellationToken);
        await db.Entry(session).ReloadAsync(cancellationToken);
        if (affected == 0) {
            logger.LogInformation("Viva payment {PaymentId} transition to {Status} skipped; current status is {Current}.", session.Id, status, session.Status);
            return false;
        }
        await eventHandler.OnStatusChangedAsync(session.ToModel(), cancellationToken);
        return true;
    }

    private static bool HasTransaction(DbPaymentSession session) =>
        !session.PaymentIntentId.StartsWith(PendingPrefix, StringComparison.Ordinal) && !session.PaymentIntentId.StartsWith(ChargingPrefix, StringComparison.Ordinal);

    /// <summary>Maps a Viva transaction status to the payment status; <c>null</c> while it is not final.</summary>
    public static PaymentAuthorizationStatus? MapStatus(VivaTransaction transaction) => transaction.StatusId?.Trim().ToUpperInvariant() switch {
        "F" => PaymentAuthorizationStatus.Succeeded,
        "E" => PaymentAuthorizationStatus.Failed,
        "X" => PaymentAuthorizationStatus.Cancelled,
        _ => null
    };

    /// <summary>Reads the event type, transaction id and merchant reference of a Viva webhook payload.</summary>
    public static bool TryReadEvent(string json, out int eventTypeId, out string? transactionId, out string? merchantTrns) {
        eventTypeId = 0;
        transactionId = null;
        merchantTrns = null;
        try {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!TryGetProperty(root, "EventTypeId", out var type) || !type.TryGetInt32(out eventTypeId)) {
                return false;
            }
            if (!TryGetProperty(root, "EventData", out var data) || data.ValueKind != JsonValueKind.Object) {
                return false;
            }
            transactionId = TryGetProperty(data, "TransactionId", out var id) ? id.ToString() : null;
            merchantTrns = TryGetProperty(data, "MerchantTrns", out var trns) && trns.ValueKind == JsonValueKind.String ? trns.GetString() : null;
            return !string.IsNullOrWhiteSpace(transactionId);
        } catch (JsonException) {
            return false;
        }
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value) {
        foreach (var property in element.EnumerateObject()) {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) {
                value = property.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static long ToMinorUnits(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
