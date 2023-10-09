using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Extensions;
using Microsoft.Extensions.Options;

namespace Indice.Services;

/// <summary>
/// SendGrid implementation for the email service abstraction.
/// https://docs.sendgrid.com/api-reference/mail-send/mail-send
/// </summary>
public class EmailServiceSendGrid : IEmailService
{
    /// <summary>Creates a new instance of <see cref="EmailServiceSendGrid"/>.</summary>
    /// <param name="settings">An instance of <see cref="EmailServiceSendGridSettings"/> used to initialize the service.</param>
    /// <param name="httpClient">The HTTP client to use (DI managed)</param>
    /// <param name="htmlRenderingEngine">This is an abstraction for the rendering engine.</param>
    public EmailServiceSendGrid(
        IOptionsSnapshot<EmailServiceSendGridSettings> settings,
        HttpClient httpClient,
        IHtmlRenderingEngine htmlRenderingEngine) {
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        HtmlRenderingEngine = htmlRenderingEngine ?? throw new ArgumentNullException(nameof(htmlRenderingEngine));
        if (HttpClient.BaseAddress == null) {
            HttpClient.BaseAddress = new Uri(Settings.Api.TrimEnd('/') + "/");
        }
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Settings.ApiKey
            ?? throw new ArgumentNullException(nameof(Settings.ApiKey)));
    }

    private EmailServiceSendGridSettings Settings { get; }
    private HttpClient HttpClient { get; }
    /// <inheritdoc/>
    public IHtmlRenderingEngine HtmlRenderingEngine { get; }

    /// <inheritdoc/>
    public async Task SendAsync(string[] recipients, string subject, string body, EmailAttachment[] attachments = null, EmailSender from = null) {
        var bccRecipients = (Settings.BccRecipients ?? "").Split(';', ',', StringSplitOptions.RemoveEmptyEntries);
        var request = new SendGridRequest {
            From = new SendGridEmailAddress {
                Email = from?.Address ?? Settings.Sender,
                Name = from?.DisplayName ?? Settings.SenderName
            },
            Subject = subject,
            Personalizations = new Personalizations {
                To = recipients.Select(x => new SendGridEmailAddress {
                    Email = x
                }).ToList(),
                Bcc = bccRecipients.Select(x => new SendGridEmailAddress {
                    Email = x
                }).ToList()
            },
            Attachments = attachments is { Length: > 0 }
                ? attachments.Select(x => new SendGridAttachment {
                    Filename = x.FileName,
                    Type = FileExtensions.GetMimeType(Path.GetExtension(x.FileName)),
                    Content = Convert.ToBase64String(x.Data)
                }).ToList()
                : null,
            Content = new List<SendGridContent> {
                new () {
                    Value = body
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var response = await HttpClient.PostAsync("mail/send", new StringContent(requestJson, Encoding.UTF8, MediaTypeNames.Application.Json));
        if (!response.IsSuccessStatusCode) {
            var content = await response.Content.ReadAsStringAsync();
            throw new SendGridException($"SendGrid service could not send email to recipients '{string.Join(", ", recipients)}'. Error is: '{content}'.");
        }
    }
}

/// <summary>Custom settings that are used to send emails via SendGrid.</summary>
public class EmailServiceSendGridSettings
{
    /// <summary>The configuration section name.</summary>
    public const string Name = "SendGrid";
    /// <summary>The default sender address (ex. no-reply@indice.gr).</summary>
    public string Sender { get; set; }
    /// <summary>The default sender name (ex. INDICE OE)</summary>
    public string SenderName { get; set; }
    /// <summary>Optional email addresses that are always added as blind carbon copy recipients.</summary>
    public string BccRecipients { get; set; }
    /// <summary>The SendGrid API key.</summary>
    public string ApiKey { get; set; }
    /// <summary>The SendGrid API URL (ex. https://api.sendgrid.com/v3/).</summary>
    public string Api { get; set; } = "https://api.sendgrid.com/v3/";
}

/// <summary>Exception for SendGrid email service failure.</summary>
public class SendGridException : Exception
{
    /// <inheritdoc />
    public SendGridException() {

    }
    /// <inheritdoc />
    public SendGridException(string message) : base(message) {

    }
}

#region SendGrid models

internal class SendGridRequest
{
    public SendGridEmailAddress From { get; set; }
    public string Subject { get; set; }
    [JsonPropertyName("template_id")]
    public string TemplateId { get; set; }
    public Personalizations Personalizations { get; set; }
    public List<SendGridAttachment> Attachments { get; set; }
    public List<SendGridContent> Content { get; set; }
}

internal class SendGridEmailAddress
{
    public string Email { get; set; }
    public string Name { get; set; }
}

internal class SendGridAttachment
{
    /// <summary>The Base64 encoded content of the attachment.</summary>
    public string Content { get; set; }
    /// <summary>The attachment's filename.</summary>
    public string Filename { get; set; }
    /// <summary>The MIME type of the content you are attaching.</summary>
    public string Type { get; set; }
    /// <summary>Allowed Values: inline, attachment</summary>
    public string Disposition { get; set; } = "attachment";
}

internal class Personalizations
{
    public List<SendGridEmailAddress> To { get; set; }
    public List<SendGridEmailAddress> Cc { get; set; }
    public List<SendGridEmailAddress> Bcc { get; set; }
    [JsonPropertyName("dynamic_template_data")]
    public object TemplateData { get; set; }
}

internal class SendGridContent
{
    /// <summary>The mime type of the content you are including in your email. For example, text/plain or text/html.</summary>
    public string Type { get; set; } = "text/html";
    /// <summary>The actual content of the specified mime type that you are including in your email.</summary>
    public string Value { get; set; }
}

#endregion