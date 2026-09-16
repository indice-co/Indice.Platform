using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Services;

/// <summary>
/// WeMail service implementation for the email service abstraction.
/// Reference: https://wemail.io/docs/send
/// </summary>
public sealed class EmailServiceWeMail : IEmailService
{
    private EmailServiceWeMailSettings _settings { get; }

    private HttpClient _httpClient { get; }

    private ILogger<EmailServiceWeMail> _logger { get; }

    /// <summary>Represents the name of the Smtp service as a constant string value.</summary>
    public const string ServiceName = "WeMail";

    /// <summary>The endpoint that handles message services.</summary>
    public const string MessagesEndpoint = "messages";

    /// <inheritdoc/>
    public IHtmlRenderingEngine? HtmlRenderingEngine  { get; }

    /// <inheritdoc/>
    public EmailProvider Provider { get; }

    /// <summary>Creates a new instance of <see cref="EmailServiceWeMail"/>.</summary>
    /// <param name="settings">An instance of <see cref="EmailServiceWeMailSettings"/> used to initialize the service.</param>
    /// <param name="httpClient">The HTTP client to use (DI managed)</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <param name="htmlRenderingEngine">This is an abstraction for the rendering engine.</param>
    public EmailServiceWeMail(
        IOptionsSnapshot<EmailServiceWeMailSettings> settings,
        HttpClient httpClient,
        ILogger<EmailServiceWeMail> logger,
        IHtmlRenderingEngine htmlRenderingEngine
    ) {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Provider = new EmailProvider(ServiceName, new EmailSender(_settings.Sender, _settings.SenderName));
        HtmlRenderingEngine = htmlRenderingEngine ?? throw new ArgumentNullException(nameof(htmlRenderingEngine));
        _httpClient.BaseAddress = new Uri(_settings.Api.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string[] recipients, string subject, string? body, EmailAttachment[]? attachments = null, EmailSender? from = null) {
        var messageId = Guid.NewGuid().ToString();
        var bccRecipients = (_settings.BccRecipients ?? string.Empty)
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        using var request = new HttpRequestMessage(HttpMethod.Post, MessagesEndpoint) {
            Content = JsonContent.Create(
                 new WeMailMessageRequest {
                     From = from?.Address ?? _settings.Sender,
                     To = recipients,
                     Bcc = bccRecipients.Length > 0 ? bccRecipients : null,
                     Subject = subject,
                     Html = body,
                     Headers = new Dictionary<string, string> {
                         ["X-Message-Id"] = messageId
                     },
                     Attachments = attachments?
                         .Select(x => new WeMailAttachment {
                             Filename = x.FileName,
                             Content = Convert.ToBase64String(x.Data),
                             ContentType = FileExtensions.GetMimeType(Path.GetExtension(x.FileName))
                         })
                         .ToArray()
                 },
                 options: new JsonSerializerOptions {
                     PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                     DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                 })
        };


        using var responseMessage = await _httpClient.SendAsync(request);

        if (!responseMessage.IsSuccessStatusCode) {
            var content = await responseMessage.Content.ReadAsStringAsync();
            _logger.LogError("WeMail service could not send email to recipients '{recipients}'. Error is: '{content}'.", string.Join(", ", recipients), content);
            throw new EmailServiceException($"WeMail service could not send email to recipients '{string.Join(", ", recipients)}'. Error is: '{content}'.");
        }

        var responseJson = await responseMessage.Content.ReadAsStringAsync();
        var weMailResponse = JsonSerializer.Deserialize<WeMailSendAcknowledgement>(responseJson)!;

        return new SendReceipt(weMailResponse.Id ?? messageId, weMailResponse.CreatedAt);
    }
}

/// <summary>Custom settings that are used to send emails via WeMail REST API.</summary>
public sealed class EmailServiceWeMailSettings
{
    /// <summary>The configuration section name.</summary>
    public const string Name = EmailServiceWeMail.ServiceName;

    /// <summary>The default sender address (ex. no-reply@indice.gr).</summary>
    public string Sender { get; set; } = null!;

    /// <summary>The default sender name (ex. INDICE SA)</summary>
    public string? SenderName { get; set; }

    /// <summary>Optional email addresses that are always added as blind carbon copy recipients.</summary>
    public string? BccRecipients { get; set; }

    /// <summary>The API key.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The WeMail API URL.</summary>
    public string Api { get; set; } = "https://api.wemail.io/v3/";
}

internal sealed class WeMailMessageRequest
{
    /// <summary>The sender email address.</summary>
    public required string From { get; init; }

    /// <summary>The primary recipients.</summary>
    public required string[] To { get; init; }

    /// <summary>The CC recipients.</summary>
    public IReadOnlyList<string>? Cc { get; init; }

    /// <summary>The BCC recipients.</summary>
    public IReadOnlyList<string>? Bcc { get; init; }

    /// <summary>The email subject.</summary>
    public required string Subject { get; init; }

    /// <summary>The HTML email body.</summary>
    public required string Html { get; init; }

    /// <summary>The file attachments.</summary>
    public IReadOnlyList<WeMailAttachment>? Attachments { get; set; }

    /// <summary>Custom email headers.</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
}

internal sealed class WeMailAttachment
{
    /// <summary>The attachment file name.</summary>
    public required string Filename { get; init; }

    /// <summary>The attachment content encoded as Base64.</summary>
    public required string Content { get; init; }

    /// <summary>The MIME content type of the attachment.</summary>
    [JsonPropertyName("content_type")]
    public string? ContentType { get; init; }

    [JsonPropertyName("content_id")]
    public string? ContentId { get; init; }

    public string? Disposition { get; init; }
}

internal sealed class WeMailSendAcknowledgement
{
    /// <summary>
    /// Gets the Wemail message identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the date and time when the message was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}