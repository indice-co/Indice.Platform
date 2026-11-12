using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Indice.Extensions;
using Indice.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Indice.Services;

/// <summary>SMS service implementation using the TWILIO SMS service gateway.</summary>
public class SmsServiceTwilio : ISmsService
{
    /// <summary>The TWILIO base URL address.</summary>
    internal static readonly string TWILIO_BASE_URL = "https://api.twilio.com/2010-04-01";
    /// <summary>The <see cref="System.Net.Http.HttpClient"/>.</summary>
    protected HttpClient HttpClient { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<SmsServiceTwilio> Logger { get; }
    /// <summary>The settings required to configure the service.</summary>
    protected SmsServiceTwilioSettings Settings { get; }

    /// <inheritdoc/>
    public SmsServiceTwilio(
        HttpClient httpClient,
        IOptionsSnapshot<SmsServiceTwilioSettings> settings,
        ILogger<SmsServiceTwilio> logger) {
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentException.ThrowIfNullOrWhiteSpace(Settings.AccountSid);

        var credentials = Settings.GetCredentinals();
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        HttpClient.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
    }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string destination, string subject, string? body, SmsSender? sender = null) {
        if (string.IsNullOrWhiteSpace(destination)) {
            throw new ArgumentNullException(nameof(destination), "Recipient is empty.");
        }
        if (destination.Contains(',')) {
            throw new NotSupportedException("Only a single recipient phone number is supported.");
        }
        if (!PhoneNumber.TryParse(destination, out var phone)) {
            throw new ArgumentException("Invalid recipient phone number.", nameof(destination));
        }
        if (string.IsNullOrWhiteSpace(Settings.SenderPhoneNumber) && string.IsNullOrWhiteSpace(Settings.MessagingServiceSid)) {
            throw new ArgumentException("SenderPhoneNumber or MessagingServiceSid must be provided.");
        }

        var requestUri = $"{TWILIO_BASE_URL}/Accounts/{Settings.AccountSid}/Messages.json";

        var formFields = new Dictionary<string, string> {
            ["To"] = phone.ToString("D"),
            ["Body"] = body ?? string.Empty
        };

        // Use MessagingServiceSid if present; otherwise use From number
        if (!string.IsNullOrWhiteSpace(Settings.MessagingServiceSid)) {
            formFields["MessagingServiceSid"] = Settings.MessagingServiceSid!;
        } else {
            formFields["From"] = sender?.Id ?? Settings.SenderPhoneNumber!;
        }

        if (!string.IsNullOrWhiteSpace(Settings.WebhookUrl)) {
            formFields["StatusCallback"] = Settings.WebhookUrl!;
        }

        using var content = new FormUrlEncodedContent(formFields);

        HttpResponseMessage httpResponse;
        try {
            Logger.LogInformation("Sending SMS via Twilio to {To}", phone.ToString("D"));
            httpResponse = await HttpClient.PostAsync(requestUri, content);
        } catch (HttpRequestException ex) {
            Logger.LogError(ex, "HTTP request error occurred during SMS delivery");
            throw new SmsServiceException("HTTP request error occurred during SMS delivery", ex);
        } catch (TaskCanceledException ex) {
            Logger.LogError(ex, "SMS delivery request timed out");
            throw new SmsServiceException("SMS delivery request timed out", ex);
        } catch (Exception ex) {
            Logger.LogError(ex, "Unexpected error during SMS delivery");
            throw new SmsServiceException("Unexpected error during SMS delivery", ex);
        }

        var responseContent = await httpResponse.Content.ReadAsStringAsync();
        if (!httpResponse.IsSuccessStatusCode) {
            Logger.LogInformation("SMS delivery failed: {StatusCode} - {Response}", httpResponse.StatusCode, responseContent);
            throw new SmsServiceException($"Twilio SMS delivery failed: {httpResponse.StatusCode} - {responseContent}");
        }

        var response = JsonSerializer.Deserialize<TwilioSmsResponse>(responseContent, GetJsonSerializerOptions())!;
        if (!string.IsNullOrWhiteSpace(response.ErrorCode)) {
            Logger.LogInformation("Twilio responded with error: {Error}", response.ErrorMessage);
            throw new SmsServiceException($"Twilio error: {response.ErrorMessage}");
        }

        Logger.LogInformation("SMS sent successfully with SID: {Sid}", response.Sid);
        return new SendReceipt(response.Sid!, DateTimeOffset.UtcNow);
    }

    /// <summary>Checks the implementation if supports the given <paramref name="deliveryChannel"/>.</summary>
    /// <param name="deliveryChannel">A string representing the delivery channel. i.e 'SMS'</param>
    /// <returns></returns>
    public bool Supports(string deliveryChannel) => "SMS".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase);

    /// <summary>Get default JSON serializer options.</summary>
    protected static JsonSerializerOptions GetJsonSerializerOptions() => new JsonSerializerOptions {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

/// <summary>Extra settings class for configuring TWILIO SMS service client. </summary>
public class SmsServiceTwilioSettings
{
    /// <summary>Key in the configuration.</summary>
    public static readonly string Name = "Sms";
    /// <summary>The API key.</summary>
    public string? ApiKey { get; set; }
    /// <summary>The Secret.</summary>
    public string? Secret { get; set; }
    /// <summary>The Account Sid.</summary>
    public string? AccountSid { get; set; }
    /// <summary>The Auth Token.</summary>
    public string? AuthToken { get; set; }
    /// <summary>The Sender Phone Number.</summary>
    public string? SenderPhoneNumber { get; set; }
    /// <summary>The Messaging Service Sid.</summary>
    public string? MessagingServiceSid { get; set; }
    /// <summary>The Auth Token used to validate incoming Twilio webhook signatures. Defaults to <see cref="AuthToken"/> when omitted.</summary>
    public string? WebhookAuthToken { get; set; }
    /// <summary>The absolute webhook URL registered in Twilio and used for signature validation.</summary>
    public string? WebhookUrl { get; set; }

    /// <summary>Gets the credentials in Base64UrlSafe format.</summary>
    public string GetCredentinals() {

        var credentials = this switch {
            _ when !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Secret) => $"{Uri.EscapeDataString(ApiKey)}:{Uri.EscapeDataString(Secret)}",
            _ when !string.IsNullOrWhiteSpace(AccountSid) && !string.IsNullOrWhiteSpace(AuthToken) => $"{Uri.EscapeDataString(AccountSid)}:{Uri.EscapeDataString(AuthToken!)}",
            _ => throw new ArgumentException("At least one of the parameter pairs (ApiKey, Secret) or (AuthToken) must be specified.")
        };
        return Encoding.ASCII.GetBytes(credentials).ToBase64UrlSafe();
    }
}

/// <inheritdoc/>
public sealed class TwilioWebhookHttpHandler : IWebhookHttpHandler {
    private readonly SmsServiceTwilioSettings _settings;
    private static readonly HashSet<string> SucceededStatuses = new(StringComparer.OrdinalIgnoreCase) {
        "Status:queued",
        "Status:sending",
        "Status:sent",
        "Status:delivered",
        "Status:receiving",
        "Status:received",
        "Status:accepted",
        "Status:scheduled",
        "Status:read"
    };

    /// <inheritdoc/>
    public TwilioWebhookHttpHandler(IOptionsSnapshot<SmsServiceTwilioSettings> settings) {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc/>
    public async Task<WebhookHttpResult> ParseAsync(
        IDictionary<string, StringValues> requestHeaders,
        IEnumerable<KeyValuePair<string, StringValues>> requestParameters,
        Stream requestBody,
        CancellationToken cancellationToken) {
        var (parameters, signatureParameters) = await BuildParametersAsync(requestHeaders, requestParameters, requestBody, cancellationToken);

        if (!IsTwilioSignatureValid(requestHeaders, signatureParameters)) {
            return WebhookHttpResult.ForbiddenRequest("Invalid Twilio signature.");
        }
        
        var messageId = FirstNonEmpty(parameters, "MessageSid", "SmsSid", "Sid");
        var recipient = FirstNonEmpty(parameters, "To", "to");

        if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(recipient)) {
            return WebhookHttpResult.BadRequest("Twilio webhook requires a message identifier and a recipient.");
        }

        var sender = FirstNonEmpty(parameters, "From", "from");
        var status = FirstNonEmpty(parameters, "MessageStatus", "SmsStatus", "SmsEvent");
        var body = FirstNonEmpty(parameters, "Body", "body");
        var timestamp = ParseTimestamp(parameters);
        var eventType = !string.IsNullOrWhiteSpace(status)
            ? $"Status:{status}"
            : !string.IsNullOrWhiteSpace(body)
                ? "InboundSms"
                : "SmsWebhook";

        var payload = new JsonObject();
        foreach (var parameter in parameters) {
            payload[parameter.Key] = parameter.Value;
        }

        var eventId = !string.IsNullOrWhiteSpace(status)
            ? $"{messageId}:{status}:{timestamp:O}"
            : $"{messageId}:{timestamp:O}";

        return WebhookHttpResult.Ok(new List<WebhookEvent> {
            new() {
                Provider = "Twilio",
                EventType = eventType,
                EventId = eventId,
                MessageId = messageId,
                Recipient = recipient,
                Subject = sender,
                Timestamp = timestamp,
                Payload = payload,
                Succeeded = EventSucceeded(eventType),
                Channel = "SMS"
            }
        }, 204);
    }

    private static bool EventSucceeded(string eventType) {
        return SucceededStatuses.Contains(eventType);
    }
    private bool IsTwilioSignatureValid(
        IDictionary<string, StringValues> requestHeaders,
        IDictionary<string, string> parameters) {
        if (string.IsNullOrWhiteSpace(_settings.WebhookAuthToken)) {
            return true; // Skip signature validation if webhook auth token isn't set.
        }
        if (!requestHeaders.TryGetValue("X-Twilio-Signature", out var signatureHeaderValues)) {
            return false;
        }

        var signature = signatureHeaderValues.ToString();
        if (string.IsNullOrWhiteSpace(signature)) {
            return false;
        }

        var authToken = _settings.WebhookAuthToken;
        var webhookUrl = _settings.WebhookUrl;
        if (string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(webhookUrl)) {
            return false;
        }

        var payload = BuildSignedPayload(webhookUrl, parameters);
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(authToken));
        var expectedSignature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));

        return FixedTimeEquals(signature.Trim(), expectedSignature);
    }

    private static async Task<(Dictionary<string, string> Parameters, Dictionary<string, string> SignatureParameters)> BuildParametersAsync(
        IDictionary<string, StringValues> requestHeaders,
        IEnumerable<KeyValuePair<string, StringValues>> requestParameters,
        Stream requestBody,
        CancellationToken cancellationToken) {
        var parameters = requestParameters.ToDictionary(p => p.Key, p => p.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var signatureParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (requestBody is null || requestBody == Stream.Null) {
            return (parameters, signatureParameters);
        }
        if (requestBody.CanSeek && requestBody.Length == 0) {
            return (parameters, signatureParameters);
        }

        if (requestBody.CanSeek) {
            requestBody.Position = 0;
        }

        using var reader = new StreamReader(requestBody, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body)) {
            return (parameters, signatureParameters);
        }

        var contentType = requestHeaders.TryGetValue("Content-Type", out var contentTypeValues)
            ? contentTypeValues.ToString()
            : string.Empty;

        if (IsFormUrlEncoded(contentType)) {
            foreach (var pair in ParseFormUrlEncoded(body)) {
                parameters[pair.Key] = pair.Value;
                signatureParameters[pair.Key] = pair.Value;
            }
            return (parameters, signatureParameters);
        }

        try {
            var json = JsonNode.Parse(body) as JsonObject;
            if (json is null) {
                return (parameters, signatureParameters);
            }

            foreach (var item in json.Where(item => item.Value is not null)) {
                parameters[item.Key] = item.Value!.ToString();
            }
        } catch (JsonException) {
            // Ignore malformed/non-JSON body and fallback to query parameters only.
        }

        return (parameters, signatureParameters);
    }

    private static bool IsFormUrlEncoded(string? contentType) {
        return !string.IsNullOrWhiteSpace(contentType) &&
               contentType.IndexOf(MediaTypeNames.Application.FormUrlEncoded, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static IEnumerable<KeyValuePair<string, string>> ParseFormUrlEncoded(string body) {
        foreach (var segment in body.Split('&', StringSplitOptions.RemoveEmptyEntries)) {
            var separatorIndex = segment.IndexOf('=');
            var encodedKey = separatorIndex >= 0 ? segment[..separatorIndex] : segment;
            var encodedValue = separatorIndex >= 0 ? segment[(separatorIndex + 1)..] : string.Empty;
            var key = Uri.UnescapeDataString(encodedKey.Replace('+', ' '));
            var value = Uri.UnescapeDataString(encodedValue.Replace('+', ' '));
            if (!string.IsNullOrWhiteSpace(key)) {
                yield return new KeyValuePair<string, string>(key, value);
            }
        }
    }

    private static string BuildSignedPayload(string webhookUrl, IDictionary<string, string> parameters) {
        var builder = new StringBuilder(webhookUrl);
        foreach (var parameter in parameters.OrderBy(item => item.Key, StringComparer.Ordinal)) {
            builder.Append(parameter.Key).Append(parameter.Value);
        }
        return builder.ToString();
    }

    private static DateTimeOffset ParseTimestamp(IDictionary<string, string> parameters) {
        var timestamp = FirstNonEmpty(parameters, "Timestamp");
        if (!string.IsNullOrWhiteSpace(timestamp) &&
            long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epoch) &&
            epoch >= DateTimeOffset.MinValue.ToUnixTimeSeconds() &&
            epoch <= DateTimeOffset.MaxValue.ToUnixTimeSeconds()) {
            return DateTimeOffset.FromUnixTimeSeconds(epoch);
        }

        var date = FirstNonEmpty(parameters, "DateCreated", "DateSent", "date_created", "date_sent");
        if (!string.IsNullOrWhiteSpace(date) &&
            DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedDate)) {
            return parsedDate;
        }

        return DateTimeOffset.UtcNow;
    }

    private static string FirstNonEmpty(IDictionary<string, string> parameters, params string[] keys) {
        foreach (var key in keys) {
            if (parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) {
                return value;
            }
        }
        return string.Empty;
    }

    private static bool FixedTimeEquals(string left, string right) {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

internal class TwilioSmsResponse
{
    [JsonPropertyName("sid")]
    public string? Sid { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("to")]
    public string? To { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("date_created")]
    public string? DateCreated { get; set; }

    [JsonPropertyName("date_sent")]
    public string? DateSent { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("subresource_uris")]
    public Dictionary<string, string>? SubresourceUris { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("code")]
    public int? Code { get; set; }
}
