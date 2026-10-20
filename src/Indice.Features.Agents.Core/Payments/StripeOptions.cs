namespace Indice.Features.Agents.Core.Payments;

/// <summary>Stripe settings for the chat payment flow (card element + manual capture). Bound from the <c>Stripe</c> configuration section.</summary>
/// <remarks>Keep <see cref="SecretKey"/> and <see cref="WebhookSecret"/> server-side (user-secrets / Key Vault).</remarks>
public class StripeOptions
{
    /// <summary>The configuration section name.</summary>
    public const string Name = "Stripe";
    /// <summary>The publishable key (<c>pk_...</c>) handed to the browser.</summary>
    public string? PublishableKey { get; set; }
    /// <summary>The secret API key (<c>sk_...</c>).</summary>
    public string? SecretKey { get; set; }
    /// <summary>The webhook endpoint signing secret (<c>whsec_...</c>).</summary>
    public string? WebhookSecret { get; set; }
    /// <summary>Three-letter ISO currency code. Defaults to <c>eur</c>.</summary>
    public string Currency { get; set; } = "eur";
    /// <summary>The pre-authorization (hold) amount in major currency units. Defaults to 50.</summary>
    public decimal DefaultHoldAmount { get; set; } = 50m;
    /// <summary>Optional display order of the Payment Element methods (e.g. <c>apple_pay</c>, <c>google_pay</c>, <c>card</c>). The enabled methods themselves are managed in the Stripe Dashboard.</summary>
    public string[]? PaymentMethodOrder { get; set; }
}
