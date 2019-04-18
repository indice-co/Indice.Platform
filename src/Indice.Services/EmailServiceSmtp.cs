using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Indice.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.StaticFiles;
using MimeKit;
using MimeKit.Text;

namespace Indice.Services
{
    /// <summary>
    /// Simple SMTP implementation of the <see cref="IEmailService"/> with no templating support.
    /// </summary>
    public class EmailServiceSmtp : IEmailService
    {
        private readonly FileExtensionContentTypeProvider _fileExtensionContentTypeProvider;
        private readonly EmailServiceSettings _settings;

        /// <summary>
        /// Cconstructs the service.
        /// </summary>
        /// <param name="settings">The SMTP settings to use.</param>
        /// <param name="fileExtensionContentTypeProvider">Provides a mapping between file extensions and MIME types.</param>
        public EmailServiceSmtp(EmailServiceSettings settings, FileExtensionContentTypeProvider fileExtensionContentTypeProvider) {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _fileExtensionContentTypeProvider = fileExtensionContentTypeProvider ?? throw new ArgumentNullException(nameof(fileExtensionContentTypeProvider));
        }

        /// <inheritdoc/>
        public async Task SendAsync(string[] recipients, string subject, string body, FileAttachment[] attachments = null) => await SendAsync<object>(recipients, subject, body, null, null);

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
                    if (!_fileExtensionContentTypeProvider.TryGetContentType(attachment.FileName, out var contentType)) {
                        continue;
                    }
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
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.Auto);
                if (!string.IsNullOrEmpty(_settings.Username)) {
                    client.Authenticate(_settings.Username, _settings.Password);
                }
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

        /// <inheritdoc/>
        public Task SendAsync<TModel>(Action<EmailMessageBuilder<TModel>> configureMessage) where TModel : class {
            if (configureMessage == null) {
                throw new ArgumentNullException(nameof(configureMessage));
            }
            throw new NotImplementedException();
        }

        public Task SendAsync(Action<EmailMessageBuilder> configureMessage) {
            if (configureMessage == null) {
                throw new ArgumentNullException(nameof(configureMessage));
            }
            throw new NotImplementedException();
        }
    }
}
