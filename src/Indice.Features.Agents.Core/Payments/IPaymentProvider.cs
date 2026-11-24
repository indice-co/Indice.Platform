namespace Indice.Features.Agents.Core.Payments;

/// <summary>Well-known payment provider names. The name is what the user picks as a payment method and what is stored on the payment attempt.</summary>
public static class PaymentProviders
{
    /// <summary>Stripe (Payment Element).</summary>
    public const string Stripe = "Stripe";
    /// <summary>Viva Wallet (Native Checkout v2).</summary>
    public const string Viva = "Viva";
    /// <summary>All the providers known to the platform.</summary>
    public static readonly IReadOnlyList<string> All = [Stripe, Viva];
}

/// <summary>A payment provider able to create the payment attempt of a case. A case is paid once, whatever the provider.</summary>
public interface IPaymentProvider
{
    /// <summary>The provider name (see <see cref="PaymentProviders"/>).</summary>
    string Name { get; }
    /// <summary>Creates a payment attempt with this provider for the case, or returns the live attempt when one exists (idempotent).</summary>
    /// <param name="userId">The paying user.</param>
    /// <param name="caseId">The case the payment is for.</param>
    /// <param name="tenantId">The tenant the case belongs to.</param>
    /// <param name="amount">The amount to be charged.</param>
    /// <param name="conversationId">The chat the payment is started from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PaymentSessionModel?> CreateOrReuseAttemptAsync(string userId, string caseId, string tenantId, decimal amount, Guid conversationId, CancellationToken cancellationToken = default);
}
