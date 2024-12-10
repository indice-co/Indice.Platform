﻿using Indice.Extensions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using MimeKit.Utils;

namespace Indice.Services;

/// <summary>Simple SMTP implementation of the <see cref="IEmailService"/> with no template support.</summary>
public class EmailServiceSmtp : IEmailService
{
    /// <summary>Creates a new instance of <see cref="EmailServiceSmtp"/>.</summary>
    /// <param name="settings">The SMTP settings to use.</param>
    /// <param name="htmlRenderingEngine">This is an abstraction for the rendering engine.</param>
    public EmailServiceSmtp(
        IOptionsSnapshot<EmailServiceSettings> settings,
        IHtmlRenderingEngine htmlRenderingEngine
    ) {
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        HtmlRenderingEngine = htmlRenderingEngine ?? throw new ArgumentNullException(nameof(htmlRenderingEngine));
    }

    private EmailServiceSettings Settings { get; }
    /// <inheritdoc/>
    public IHtmlRenderingEngine HtmlRenderingEngine { get; }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string[] recipients, string subject, string? body, EmailAttachment[]? attachments = null, EmailSender? from = null) {
        var messageId = MimeUtils.GenerateMessageId();
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(from?.DisplayName ?? Settings.SenderName, from?.Address ?? Settings.Sender));
        message.To.AddRange(recipients.Select(InternetAddress.Parse));
        if (!string.IsNullOrWhiteSpace(Settings.BccRecipients)) {
            var bccRecipients = Settings.BccRecipients.Split(',', ';');
            message.Bcc.AddRange(bccRecipients.Select(InternetAddress.Parse));
        }
        message.Subject = subject;
        message.MessageId = messageId;
        var bodyPart = new TextPart(TextFormat.Html) {
            Text = body
        };
        if (attachments?.Length > 0) {
            var multipart = new Multipart("mixed") {
                bodyPart
            };
            foreach (var attachment in attachments) {
                var contentType = FileExtensions.GetMimeType(Path.GetExtension(attachment.FileName));
                var contentTypeParts = contentType.Split('/');
                var attachmentPart = new MimePart(contentTypeParts[0], contentTypeParts[1]) {
                    Content = new MimeContent(new MemoryStream(attachment.Data)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = attachment.FileName
                };
                multipart.Add(attachmentPart);
                message.Body = multipart;
            }
        } else {
            message.Body = bodyPart;
        }
        using (var client = new SmtpClient()) {
            // If UseSSL = true then you need to provide certificate in order to send the email, or else you get security exception.
            var useSSL = Settings.UseSSL && client.ClientCertificates != null && client.ClientCertificates.Count > 0;
            // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS).
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.CheckCertificateRevocation = Settings.CheckCertificateRevocation;
            // Since we don't have an OAuth2 token, disable the XOAUTH2 authentication mechanism.
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            // https://www.stevejgordon.co.uk/how-to-send-emails-in-asp-net-core-1-0
            // https://portal.smartertools.com/kb/a2862/smtp-settings-for-outlook365-and-gmail.aspx
            // Only needed if the SMTP server requires authentication.
            await client.ConnectAsync(Settings.SmtpHost, Settings.SmtpPort, (MailKit.Security.SecureSocketOptions)(int)Settings.SecureSocket);
            if (!string.IsNullOrEmpty(Settings.Username)) {
                client.Authenticate(Settings.Username, Settings.Password);
            }
            var response = await client.SendAsync(message);
            if (!string.IsNullOrWhiteSpace(response)) {
                // 250 Ok: queued as Yo60h6C5ScGPeP5fUWU3K
                
            }
            await client.DisconnectAsync(true);
        }
        return new SendReceipt(messageId, DateTimeOffset.UtcNow);
    }
}
