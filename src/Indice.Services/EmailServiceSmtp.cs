using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Indice.Configuration;
using Indice.Extensions;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace Indice.Services
{
    /// <summary>
    /// Simple SMTP implementation of the <see cref="IEmailService"/> with no templating support.
    /// </summary>
    public class EmailServiceSmtp : IEmailService
    {
        private readonly EmailServiceSettings _settings;

        /// <summary>
        /// Cconstructs the service.
        /// </summary>
        /// <param name="settings">The SMTP settings to use.</param>
        public EmailServiceSmtp(EmailServiceSettings settings) => _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        /// <inheritdoc/>
        public async Task SendAsync(string[] recipients, string subject, string body, FileAttachment[] attachments = null) => await SendAsync<object>(recipients, subject, body, null, null, attachments);

        /// <inheritdoc/>
        public async Task SendAsync<TModel>(string[] recipients, string subject, string body, string template, TModel data, FileAttachment[] attachments = null) where TModel : class {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.Sender));
            message.To.AddRange(recipients.Select(recipient => InternetAddress.Parse(recipient)));
            if (!string.IsNullOrWhiteSpace(_settings.BccRecipients)) {
                var bccRecipients = _settings.BccRecipients.Split(',', ';');
                message.Bcc.AddRange(bccRecipients.Select(recipient => InternetAddress.Parse(recipient)));
            }
            message.Subject = subject;
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
                var useSSL = _settings.UseSSL && client.ClientCertificates != null && client.ClientCertificates.Count > 0;
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS).
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                // Since we don't have an OAuth2 token, disable the XOAUTH2 authentication mechanism.
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                // https://www.stevejgordon.co.uk/how-to-send-emails-in-asp-net-core-1-0
                // https://portal.smartertools.com/kb/a2862/smtp-settings-for-outlook365-and-gmail.aspx
                // Only needed if the SMTP server requires authentication.
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, (MailKit.Security.SecureSocketOptions)(int)_settings.SecureSocket);
                if (!string.IsNullOrEmpty(_settings.Username)) {
                    client.Authenticate(_settings.Username, _settings.Password);
                }
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}
