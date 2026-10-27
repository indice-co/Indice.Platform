namespace Indice.Features.Agents.Core.Payments;

/// <summary>The Viva Wallet environment.</summary>
public enum VivaEnvironment
{
    /// <summary>The demo (sandbox) environment.</summary>
    Demo,
    /// <summary>The live environment.</summary>
    Production
}

/// <summary>Viva Wallet settings for the chat payment flow (Native Checkout v2, direct charge). Bound from the <c>Viva</c> configuration section.</summary>
/// <remarks>Keep <see cref="ClientSecret"/> and <see cref="ApiKey"/> server-side (user-secrets / Key Vault).</remarks>
public class VivaOptions
{
    /// <summary>The configuration section name.</summary>
    public const string Name = "Viva";
    /// <summary>The environment; selects the default URLs. Defaults to <see cref="VivaEnvironment.Demo"/>.</summary>
    public VivaEnvironment Environment { get; set; } = VivaEnvironment.Demo;
    /// <summary>The Smart Checkout / Native Checkout OAuth2 client id.</summary>
    public string? ClientId { get; set; }
    /// <summary>The OAuth2 client secret.</summary>
    public string? ClientSecret { get; set; }
    /// <summary>The merchant id (Basic auth user for the webhook verification key).</summary>
    public string? MerchantId { get; set; }
    /// <summary>The merchant API key (Basic auth password for the webhook verification key).</summary>
    public string? ApiKey { get; set; }
    /// <summary>The 4-digit payment source code configured in the Viva dashboard.</summary>
    public string? SourceCode { get; set; }
    /// <summary>Three-letter ISO currency code. Defaults to <c>EUR</c>.</summary>
    public string Currency { get; set; } = "EUR";
    /// <summary>Optional override of the accounts (token) URL.</summary>
    public string? AccountsUrl { get; set; }
    /// <summary>Optional override of the API URL (also the <c>baseURL</c> of the Native Checkout JS).</summary>
    public string? ApiUrl { get; set; }
    /// <summary>Optional override of the web URL (Native Checkout JS and webhook verification key).</summary>
    public string? WebUrl { get; set; }
    /// <summary>Minimum time between two reconciliations of a pending attempt against Viva. Defaults to 5 seconds.</summary>
    public TimeSpan ReconciliationInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>The effective accounts URL.</summary>
    public string GetAccountsUrl() => (AccountsUrl ?? (Environment == VivaEnvironment.Production ? "https://accounts.vivapayments.com" : "https://demo-accounts.vivapayments.com")).TrimEnd('/');
    /// <summary>The effective API URL.</summary>
    public string GetApiUrl() => (ApiUrl ?? (Environment == VivaEnvironment.Production ? "https://api.vivapayments.com" : "https://demo-api.vivapayments.com")).TrimEnd('/');
    /// <summary>The effective web URL.</summary>
    public string GetWebUrl() => (WebUrl ?? (Environment == VivaEnvironment.Production ? "https://www.vivapayments.com" : "https://demo.vivapayments.com")).TrimEnd('/');
    /// <summary>The Native Checkout v2 script URL.</summary>
    public string GetScriptUrl() => $"{GetWebUrl()}/web/checkout/v2/js";

    /// <summary>The ISO 4217 numeric code of <see cref="Currency"/>, as required by Viva.</summary>
    /// <exception cref="InvalidOperationException">The currency is not supported.</exception>
    public int GetCurrencyCode() => Currency.ToUpperInvariant() switch {
        "EUR" => 978,
        "GBP" => 826,
        "USD" => 840,
        "RON" => 946,
        "PLN" => 985,
        "CZK" => 203,
        "HUF" => 348,
        "SEK" => 752,
        "DKK" => 208,
        "BGN" => 975,
        "CHF" => 756,
        _ => throw new InvalidOperationException($"Viva currency '{Currency}' is not supported.")
    };
}
