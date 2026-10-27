namespace Indice.Features.Agents.Core.Payments;

/// <summary>Lifecycle of a case payment.</summary>
public enum PaymentAuthorizationStatus
{
    /// <summary>The PaymentIntent was created and awaits confirmation.</summary>
    Created,
    /// <summary>The payment was charged.</summary>
    Succeeded,
    /// <summary>The payment failed.</summary>
    Failed,
    /// <summary>The PaymentIntent was cancelled before payment.</summary>
    Cancelled
}

/// <summary>A payment attempt as returned to the client.</summary>
public class PaymentSessionModel
{
    /// <summary>The payment session id.</summary>
    public Guid Id { get; set; }
    /// <summary>The case this payment belongs to.</summary>
    public string CaseId { get; set; } = null!;
    /// <summary>The chat the payment was started from.</summary>
    public Guid ConversationId { get; set; }
    /// <summary>The payment provider (see <see cref="PaymentProviders"/>).</summary>
    public string Provider { get; set; } = PaymentProviders.Stripe;
    /// <summary>The PaymentIntent <c>client_secret</c> the card is confirmed against.</summary>
    public string? PaymentSessionSecret { get; set; }
    /// <summary>The authorization status.</summary>
    public PaymentAuthorizationStatus PaymentAuthorizationStatus { get; set; }
    /// <summary>The amount charged, in major currency units.</summary>
    public decimal Amount { get; set; }
    /// <summary>Three-letter ISO currency code.</summary>
    public string Currency { get; set; } = null!;
}

/// <summary>Request to charge a Viva attempt with a Native Checkout card token.</summary>
public class VivaChargeRequestModel
{
    /// <summary>The token returned by <c>VivaPayments.cards.requestToken</c>.</summary>
    public string ChargeToken { get; set; } = null!;
}

/// <summary>The response to Viva's webhook URL verification.</summary>
/// <param name="Key">The merchant webhook key.</param>
public record VivaWebhookKey([property: System.Text.Json.Serialization.JsonPropertyName("Key")] string Key);

/// <summary>The publishable key response.</summary>
public class PaymentProviderPublicKey
{
    /// <summary>The Stripe publishable key.</summary>
    public string? PublishableKey { get; set; }
    /// <summary>Optional display order of the payment methods in the Payment Element.</summary>
    public string[]? PaymentMethodOrder { get; set; }
}
