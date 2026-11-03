using Indice.Features.Agents.Core.Payments;

namespace Indice.Features.Agents.Core.Data;

/// <summary>A payment attempt bound to a case. A case is paid once.</summary>
public class DbPaymentSession
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>The case this payment belongs to (the payment key).</summary>
    public string CaseId { get; set; } = null!;
    /// <summary>The tenant this payment belongs to.</summary>
    public string TenantId { get; set; } = null!;
    /// <summary>The chat session the payment was started from, if any.</summary>
    public Guid ConversationId { get; set; }
    /// <summary><c>sub</c> claim of the paying user.</summary>
    public string UserId { get; set; } = null!;
    /// <summary>The payment provider (see <see cref="PaymentProviders"/>). Defaults to <c>Stripe</c>.</summary>
    public string Provider { get; set; } = PaymentProviders.Stripe;
    /// <summary>The provider reference: the Stripe PaymentIntent id (<c>pi_...</c>) or the Viva transaction id (a <c>viva:{Id}</c> placeholder until charged).</summary>
    public string PaymentIntentId { get; set; } = null!;
    /// <summary>The PaymentIntent <c>client_secret</c>.</summary>
    public string? ClientSecret { get; set; }
    /// <summary>The authorization status.</summary>
    public PaymentAuthorizationStatus Status { get; set; }
    /// <summary>The amount charged, in major currency units.</summary>
    public decimal Amount { get; set; }
    /// <summary>Three-letter ISO currency code.</summary>
    public string Currency { get; set; } = "eur";
    /// <summary>The last failure reason reported by Stripe.</summary>
    public string? FailureReason { get; set; }
    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Last status change timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
