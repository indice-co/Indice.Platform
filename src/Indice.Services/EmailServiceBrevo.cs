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
    /// <summary>Represents the name of the Brevo service as a constant string value.</summary>
    public const string ServiceName = "Brevo";

    /// <summary>Creates a new instance of <see cref="EmailServiceBrevo"/>.</summary>
    /// <param name="settings">An instance of <see cref="EmailServiceBrevoSettings"/> used to initialize the service.</param>
    /// <param name="httpClient">The HTTP client to use (DI managed)</param>
    /// <param name="htmlRenderingEngine">This is an abstraction for the rendering engine.</param>
    public EmailServiceBrevo(
        IOptionsSnapshot<EmailServiceBrevoSettings> settings,
        HttpClient httpClient,
        IHtmlRenderingEngine htmlRenderingEngine) {
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        Provider = new EmailProvider(ServiceName, new EmailSender(Settings.Sender!, Settings.SenderName));
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
    public EmailProvider Provider { get; }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string[] recipients, string subject, string? body, EmailAttachment[]? attachments = null, EmailSender? from = null) {
        var serializerOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var bccRecipients = string.IsNullOrEmpty(Settings.BccRecipients)
            ? null
            : (Settings.BccRecipients ?? "").Split(';', ',', StringSplitOptions.RemoveEmptyEntries).Select(x => new BrevoEmailAddress { Email = x }).ToArray();
        var request = new BrevoRequest {
            Sender = new BrevoEmailAddress {
                Email = from?.Address ?? Settings.Sender,
                Name = from?.DisplayName ?? Settings.SenderName
            },
            Subject = subject,
            To = recipients.Select(recipient => new BrevoEmailAddress {
                Email = recipient
            }).ToArray(),
            Bcc = bccRecipients,
            Attachment = attachments is { Length: > 0 }
                ? attachments.Select(x => new BrevoAttachment {
                    Name = x.FileName,
                    Content = Convert.ToBase64String(x.Data)
                }).ToArray()
                : null,
            HtmlContent = body
        };

        var requestJson = JsonSerializer.Serialize(request, serializerOptions);
        var messageId = Guid.NewGuid().ToString();
        using (var content = new StringContent(requestJson, Encoding.UTF8, MediaTypeNames.Application.Json)) {
            var response = await HttpClient.PostAsync("smtp/email", content);
            var responseContentJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                throw new EmailServiceException($"Brevo service could not send email to recipients '{string.Join(", ", recipients)}'. Error is: '{responseContentJson}'.");
            }
            
            if (!string.IsNullOrWhiteSpace(responseContentJson)) {
                var responseContent = JsonSerializer.Deserialize<BrevoResponse>(responseContentJson, serializerOptions);
                messageId = responseContent?.MessageId ?? messageId;
            }
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
    /// <summary>Optional email addresses that are always added as blind carbon copy recipients.</summary>
    public string? BccRecipients { get; set; }
    /// <summary>The Brevo API key.</summary>
    public string? ApiKey { get; set; }
    /// <summary>The Brevo API URL (ex. https://api.brevo.com/v3/).</summary>
    public string Api { get; set; } = "https://api.brevo.com/v3/";
}

#region Brevo models

internal class BrevoRequest
{
    public BrevoEmailAddress? Sender { get; set; }
    public BrevoEmailAddress[]? To { get; set; }
    public BrevoEmailAddress[]? Bcc { get; set; }
    public BrevoAttachment[]? Attachment { get; set; }
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

internal class BrevoAttachment
{
    public string? Content { get; set; }
    public string? Name { get; set; }
}
#endregion
