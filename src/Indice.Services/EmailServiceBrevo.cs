using System.Net.Mime;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;

namespace Indice.Services;

/// <summary>
/// Brevo implementation for the email service abstraction.
/// https://developers.brevo.com/docs/send-a-transactional-email
/// </summary>
public class EmailServiceBrevo : IEmailService
{
    /// <summary>Creates a new instance of <see cref="EmailServiceBrevo"/>.</summary>
    /// <param name="settings">An instance of <see cref="EmailServiceBrevoSettings"/> used to initialize the service.</param>
    /// <param name="httpClient">The HTTP client to use (DI managed)</param>
    /// <param name="htmlRenderingEngine">This is an abstraction for the rendering engine.</param>
    public EmailServiceBrevo(
        IOptionsSnapshot<EmailServiceBrevoSettings> settings,
        HttpClient httpClient,
        IHtmlRenderingEngine htmlRenderingEngine) {
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        HtmlRenderingEngine = htmlRenderingEngine ?? throw new ArgumentNullException(nameof(htmlRenderingEngine));
        if (HttpClient.BaseAddress == null) {
            HttpClient.BaseAddress = new Uri(Settings.Api.TrimEnd('/') + "/");
        }
        HttpClient.DefaultRequestHeaders.Add("api-key", Settings.ApiKey ?? throw new ArgumentNullException(nameof(Settings.ApiKey)));
    }

    private EmailServiceBrevoSettings Settings { get; }
    private HttpClient HttpClient { get; }
    /// <inheritdoc/>
    public IHtmlRenderingEngine HtmlRenderingEngine { get; }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string[] recipients, string subject, string? body, EmailAttachment[]? attachments = null, EmailSender? from = null) {
        var serializerOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var request = new BrevoRequest {
            Sender = new BrevoEmailAddress {
                Email = from?.Address ?? Settings.Sender,
                Name = from?.DisplayName ?? Settings.SenderName
            },
            Subject = subject,
            To = recipients.Select(recipient => new BrevoEmailAddress {
                Email = recipient
            }).ToArray(),
            HtmlContent = body
        };

        var requestJson = JsonSerializer.Serialize(request, serializerOptions);

        var response = await HttpClient.PostAsync("smtp/email", new StringContent(requestJson, Encoding.UTF8, MediaTypeNames.Application.Json));
        var contentJson = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            throw new BrevoException($"Brevo service could not send email to recipients '{string.Join(", ", recipients)}'. Error is: '{contentJson}'.");
        }
        var messageId = Guid.NewGuid().ToString();
        if (!string.IsNullOrWhiteSpace(contentJson)) {
            var content = JsonSerializer.Deserialize<BrevoResponse>(contentJson, serializerOptions);
            messageId = content?.MessageId ?? messageId;
        }

        return new SendReceipt(messageId, DateTimeOffset.UtcNow);
    }
}

/// <summary>Custom settings that are used to send emails via Brevo.</summary>
public class EmailServiceBrevoSettings
{
    /// <summary>The configuration section name.</summary>
    public const string Name = "Brevo";
    /// <summary>The default sender address (ex. no-reply@indice.gr).</summary>
    public string? Sender { get; set; }
    /// <summary>The default sender name (ex. INDICE OE)</summary>
    public string? SenderName { get; set; }
    /// <summary>The Brevo API key.</summary>
    public string? ApiKey { get; set; }
    /// <summary>The Brevo API URL (ex. https://api.brevo.com/v3/).</summary>
    public string Api { get; set; } = "https://api.brevo.com/v3/";
}

/// <summary>Exception for Brevo email service failure.</summary>
public class BrevoException : Exception
{
    /// <inheritdoc />
    public BrevoException() {

    }
    /// <inheritdoc />
    public BrevoException(string message) : base(message) {

    }
}

#region Brevo models

internal class BrevoRequest
{
    public BrevoEmailAddress? Sender { get; set; }
    public BrevoEmailAddress[]? To { get; set; }
    public string? Subject { get; set; }
    public string? HtmlContent { get; set; }
}

internal class BrevoResponse
{
    public string? MessageId { get; set; }
}


internal class BrevoEmailAddress
{
    public string? Email { get; set; }
    public string? Name { get; set; }
}
#endregion
