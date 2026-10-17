using Indice.Features.Agents.Core.Payments;

namespace Indice.Features.Agents.Core.Data;

/// <summary>A Stripe manual-capture payment attempt bound to a case. A case is paid once.</summary>
public class DbPaymentSession
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>The case this payment belongs to (the payment key).</summary>
    public string CaseId { get; set; } = string.Empty;
    /// <summary>The chat session the payment was started from, if any.</summary>
    public Guid ConversationId { get; set; }
    /// <summary><c>sub</c> claim of the paying user.</summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>The Stripe PaymentIntent id (<c>pi_...</c>).</summary>
    public string PaymentIntentId { get; set; } = string.Empty;
    /// <summary>The PaymentIntent <c>client_secret</c>.</summary>
    public string? ClientSecret { get; set; }
    /// <summary>The authorization status.</summary>
    public PaymentAuthorizationStatus Status { get; set; }
    /// <summary>The pre-auth (hold) amount in major currency units.</summary>
    public decimal FirstCaptureAmount { get; set; }
    /// <summary>The amount captured at settlement.</summary>
    public decimal? CapturedAmount { get; set; }
    /// <summary>Three-letter ISO currency code.</summary>
    public string Currency { get; set; } = "eur";
    /// <summary>The last failure reason reported by Stripe.</summary>
    public string? FailureReason { get; set; }
    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Last status change timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
