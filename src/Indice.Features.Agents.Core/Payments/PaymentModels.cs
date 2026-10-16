namespace Indice.Features.Agents.Core.Payments;

/// <summary>Lifecycle of a manual-capture pre-authorization.</summary>
public enum PaymentAuthorizationStatus
{
    /// <summary>The PaymentIntent was created and awaits card confirmation.</summary>
    Created,
    /// <summary>The hold is in place (<c>requires_capture</c>) or the payment was captured.</summary>
    Succeeded,
    /// <summary>The authorization failed.</summary>
    Failed,
    /// <summary>The PaymentIntent was cancelled and the hold released.</summary>
    Cancelled
}

/// <summary>A payment attempt as returned to the client.</summary>
public class PaymentSessionModel
{
    /// <summary>The payment session id.</summary>
    public Guid Id { get; set; }
    /// <summary>The chat this payment belongs to.</summary>
    public Guid ConversationId { get; set; }
    /// <summary>The PaymentIntent <c>client_secret</c> the card is confirmed against.</summary>
    public string? PaymentSessionSecret { get; set; }
    /// <summary>The authorization status.</summary>
    public PaymentAuthorizationStatus PaymentAuthorizationStatus { get; set; }
    /// <summary>The pre-auth (hold) amount in major currency units.</summary>
    public decimal FirstCaptureAmount { get; set; }
    /// <summary>The amount captured at settlement, if any.</summary>
    public decimal? CapturedAmount { get; set; }
    /// <summary>Three-letter ISO currency code.</summary>
    public string Currency { get; set; } = string.Empty;
}

/// <summary>The publishable key response.</summary>
public class PaymentProviderPublicKey
{
    /// <summary>The Stripe publishable key.</summary>
    public string? PublishableKey { get; set; }
}
