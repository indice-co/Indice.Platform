using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Payments;

/// <summary>An error reported by the Viva Wallet API.</summary>
public class VivaException(string message, int? statusCode = null, Exception? innerException = null) : Exception(message, innerException)
{
    /// <summary>The HTTP status code, when available.</summary>
    public int? StatusCode { get; } = statusCode;
}

/// <summary>A Viva OAuth2 access token.</summary>
/// <param name="Token">The bearer token.</param>
/// <param name="ExpiresAt">When the token expires.</param>
public record VivaAccessToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>Native Checkout v2 charge request (<c>POST /nativecheckout/v2/transactions</c>).</summary>
public class VivaChargeRequest
{
    /// <summary>The amount in minor units (cents).</summary>
    public long Amount { get; set; }
    /// <summary>Always <c>false</c>: direct charge.</summary>
    public bool Preauth { get; set; }
    /// <summary>The payment source code.</summary>
    public string SourceCode { get; set; } = null!;
    /// <summary>The token produced by <c>VivaPayments.cards.requestToken</c>.</summary>
    public string ChargeToken { get; set; } = null!;
    /// <summary>The ISO 4217 numeric currency code.</summary>
    public int CurrencyCode { get; set; }
    /// <summary>Merchant reference (the payment attempt id).</summary>
    public string? MerchantTrns { get; set; }
    /// <summary>Description shown to the customer.</summary>
    public string? CustomerTrns { get; set; }
}

/// <summary>A Viva transaction (<c>GET /checkout/v2/transactions/{id}</c>).</summary>
public class VivaTransaction
{
    /// <summary>The transaction id (not part of the payload; set by the client).</summary>
    [JsonIgnore]
    public string TransactionId { get; set; } = null!;
    /// <summary>
    /// The status: <c>F</c> finalized, <c>A</c> in progress, <c>E</c> error/declined, <c>X</c> cancelled,
    /// <c>R</c> refunded/reversed, <c>C</c> captured (pre-auth), <c>M</c> claimed, <c>MA</c>/<c>MI</c>/<c>ML</c>/<c>MW</c>/<c>MS</c> dispute states.
    /// </summary>
    public string? StatusId { get; set; }
    /// <summary>The amount in major units.</summary>
    public decimal Amount { get; set; }
    /// <summary>The merchant reference sent at charge time.</summary>
    public string? MerchantTrns { get; set; }
    /// <summary>The order code.</summary>
    public long? OrderCode { get; set; }
}

/// <summary>Thin client over the Viva Wallet REST API.</summary>
public interface IVivaClient
{
    /// <summary>A client-credentials access token (cached until shortly before it expires).</summary>
    Task<VivaAccessToken> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    /// <summary>Charges a card token; returns the transaction id.</summary>
    /// <exception cref="VivaException">The charge was rejected.</exception>
    Task<string> ChargeAsync(VivaChargeRequest request, CancellationToken cancellationToken = default);
    /// <summary>Retrieves a transaction; <c>null</c> when it does not exist.</summary>
    Task<VivaTransaction?> GetTransactionAsync(string transactionId, CancellationToken cancellationToken = default);
    /// <summary>The key returned to Viva when it verifies the webhook URL (cached).</summary>
    Task<string> GetWebhookKeyAsync(CancellationToken cancellationToken = default);
}

/// <summary>Process-wide cache of the Viva access token and webhook key (the typed client itself is transient).</summary>
public sealed class VivaTokenCache
{
    internal readonly SemaphoreSlim Lock = new(1, 1);
    internal VivaAccessToken? AccessToken { get; set; }
    internal string? WebhookKey { get; set; }
}

/// <summary>Default <see cref="IVivaClient"/> based on a typed <see cref="HttpClient"/>.</summary>
public class VivaClient(HttpClient httpClient, VivaTokenCache cache, IOptions<VivaOptions> options, TimeProvider? timeProvider = null) : IVivaClient
{
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromMinutes(1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly VivaOptions _options = options.Value;
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    /// <inheritdoc/>
    public async Task<VivaAccessToken> GetAccessTokenAsync(CancellationToken cancellationToken = default) {
        if (cache.AccessToken is { } cached && cached.ExpiresAt - ExpirySkew > _time.GetUtcNow()) {
            return cached;
        }
        await cache.Lock.WaitAsync(cancellationToken);
        try {
            if (cache.AccessToken is { } again && again.ExpiresAt - ExpirySkew > _time.GetUtcNow()) {
                return again;
            }
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.GetAccountsUrl()}/connect/token") {
                Content = new FormUrlEncodedContent([new("grant_type", "client_credentials")])
            };
            request.Headers.Authorization = Basic(_options.ClientId, _options.ClientSecret);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, "token", cancellationToken);
            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken)
                ?? throw new VivaException("Viva token response was empty.");
            cache.AccessToken = new VivaAccessToken(token.AccessToken, _time.GetUtcNow().AddSeconds(token.ExpiresIn));
            return cache.AccessToken;
        } finally {
            cache.Lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<string> ChargeAsync(VivaChargeRequest request, CancellationToken cancellationToken = default) {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{_options.GetApiUrl()}/nativecheckout/v2/transactions") {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        message.Headers.Authorization = await BearerAsync(cancellationToken);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, "charge", cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ChargeResponse>(JsonOptions, cancellationToken);
        return string.IsNullOrWhiteSpace(result?.TransactionId) ? throw new VivaException("Viva charge response has no transaction id.") : result.TransactionId;
    }

    /// <inheritdoc/>
    public async Task<VivaTransaction?> GetTransactionAsync(string transactionId, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
        using var message = new HttpRequestMessage(HttpMethod.Get, $"{_options.GetApiUrl()}/checkout/v2/transactions/{Uri.EscapeDataString(transactionId)}");
        message.Headers.Authorization = await BearerAsync(cancellationToken);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
            return null;
        }
        await EnsureSuccessAsync(response, "transaction", cancellationToken);
        var transaction = await response.Content.ReadFromJsonAsync<VivaTransaction>(JsonOptions, cancellationToken);
        if (transaction is not null) {
            transaction.TransactionId = transactionId;
        }
        return transaction;
    }

    /// <inheritdoc/>
    public async Task<string> GetWebhookKeyAsync(CancellationToken cancellationToken = default) {
        if (cache.WebhookKey is { } key) {
            return key;
        }
        using var message = new HttpRequestMessage(HttpMethod.Get, $"{_options.GetWebUrl()}/api/messages/config/token");
        message.Headers.Authorization = Basic(_options.MerchantId, _options.ApiKey);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, "webhook key", cancellationToken);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var value = json.RootElement.EnumerateObject().FirstOrDefault(x => string.Equals(x.Name, "Key", StringComparison.OrdinalIgnoreCase)).Value;
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString())) {
            throw new VivaException("Viva webhook key response has no key.");
        }
        return cache.WebhookKey = value.GetString()!;
    }

    private async Task<AuthenticationHeaderValue> BearerAsync(CancellationToken cancellationToken) =>
        new("Bearer", (await GetAccessTokenAsync(cancellationToken)).Token);

    private static AuthenticationHeaderValue Basic(string? user, string? password) =>
        new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken) {
        if (response.IsSuccessStatusCode) {
            return;
        }
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new VivaException($"Viva {operation} request failed with {(int)response.StatusCode}: {Truncate(body, 512)}", (int)response.StatusCode);
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = null!;
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class ChargeResponse
    {
        public string? TransactionId { get; set; }
    }
}
