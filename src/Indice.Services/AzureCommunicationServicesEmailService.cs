using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Indice.Extensions;
using Microsoft.Extensions.Options;
using EmailAttachmentAzure=Azure.Communication.Email.EmailAttachment;
using EmailMessageAzure=Azure.Communication.Email.EmailMessage;
namespace Indice.Services;

/// <summary>
/// Azure Communication Services implementation for the email service abstraction.
/// <a href="https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/email/send-email?tabs=windows%2Caad%2Csend-email-and-get-status-async%2Casync-client&amp;pivots=programming-language-csharp">Learn more</a>
/// </summary>
public sealed class AzureCommunicationServicesEmailService : IEmailService
{
    /// <summary>Represents the name of the AzureCommunicationServices service as a constant string value.</summary>
    public const string ServiceName = "AzureCommunicationServices";

    /// <summary>Creates a new instance of <see cref="AzureCommunicationServicesEmailService"/>.</summary>
    /// <param name="settings">An instance of <see cref="EmailServiceAzureCommsSettings"/> used to initialize the service.</param>
    /// <param name="htmlRenderingEngine">This is an abstraction for the rendering engine.</param>
    public AzureCommunicationServicesEmailService(
        IOptionsSnapshot<EmailServiceAzureCommsSettings> settings,
        IHtmlRenderingEngine htmlRenderingEngine
    ) {
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        Provider = new EmailProvider(ServiceName, new EmailSender(Settings.Sender, string.Empty));
        HtmlRenderingEngine = htmlRenderingEngine ?? throw new ArgumentNullException(nameof(htmlRenderingEngine));
        //Create client
        _emailClient = new EmailClient(new Uri(Settings.ResourceEndpoint), new ClientSecretCredential(Settings.TenantId, Settings.ClientId, Settings.ClientSecret));
    }

    private EmailServiceAzureCommsSettings Settings { get; }
    /// <inheritdoc/>
    public IHtmlRenderingEngine HtmlRenderingEngine { get; }
    /// <inheritdoc/>
    public EmailProvider Provider { get; }

    private readonly EmailClient _emailClient;

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string[] recipients, string subject, string? body, EmailAttachment[]? attachments = null, EmailSender? from = null) {
        var bccAddresses = Settings.BccRecipients?.Split(';', ',', StringSplitOptions.RemoveEmptyEntries).Select(x => {
            var parsed = new MailAddress(x.Trim());
            return new EmailAddress(parsed.Address, parsed.DisplayName);
        }) ?? [];
        //Currently not used and empty
        var ccAddresses = Enumerable.Empty<EmailAddress>();
        var emailRecipients = new EmailRecipients(
            recipients.Select(x => {
                var parsed = new MailAddress(x.Trim());
                return new EmailAddress(parsed.Address, parsed.DisplayName);
            }),
            ccAddresses,
            bccAddresses
        );

        var emailContent = new EmailContent(subject) {
            Html = body
        };
        var emailMessage = new EmailMessageAzure(from is not null ? from.Address : Provider.DefaultSender.Address, emailRecipients, emailContent);

        if (attachments is { Length: > 0 }) {
            foreach (var emailAttachment in attachments) {
                emailMessage.Attachments.Add(new EmailAttachmentAzure(
                    name: emailAttachment.FileName,
                    contentType: FileExtensions.GetMimeType(Path.GetExtension(emailAttachment.FileName)),
                    content: new BinaryData(emailAttachment.Data))
                );
            }
        }

        var operation = await _emailClient.SendAsync(Settings.WaitUntilCompleted ? WaitUntil.Completed : WaitUntil.Started, emailMessage);
        return new SendReceipt(operation.Id, DateTimeOffset.UtcNow);
    }
}

/// <summary>Custom settings that are used to send emails via Azure Communication Services.</summary>
/// <remarks>
/// To configure the sender name you have to take a look at the azure portal and it's documentation
/// because currently ACS doesn't allow you to set the sender name via API, and it has to be configured on the resource itself.
/// </remarks>
public sealed class EmailServiceAzureCommsSettings
{
    /// <summary>The configuration section name.</summary>
    public const string Name = "AzureCommunicationServices";
    /// <summary>The default sender address (ex. no-reply@indice.gr).</summary>
    [Required]
    [EmailAddress]
    public string Sender { get; set; } = null!;
    /// <summary>Optional email addresses that are always added as blind carbon copy recipients.</summary>
    public string? BccRecipients { get; set; }
    /// <summary>The Azure AD application (client) ID used for authentication.</summary>
    [Required]
    public string ClientId { get; set; } = null!;
    /// <summary>The Azure AD application (client) secret used for authentication.</summary>
    [Required]
    public string ClientSecret { get; set; } = null!;
    /// <summary>The Azure AD tenant ID used for authentication.</summary>
    [Required]
    public string TenantId { get; set; } = null!;
    /// <summary>The endpoint of the ACS resource</summary>
    [Required]
    [Url]
    public string ResourceEndpoint { get; set; } = null!;
    /// <summary>Whether to wait until the email sending operation is completed before returning the receipt. If false, the receipt will be returned immediately after the operation is started.</summary>
    /// <remarks>Usually waiting for the operation to complete is not recommended because it takes about 12 seconds for the email to be sent</remarks>
    public bool WaitUntilCompleted { get; set; } = false;
}
