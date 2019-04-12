using System;
using System.Linq;
using System.Threading.Tasks;
using Indice.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace Indice.Services
{
    /// <summary>
    /// Simple SMTP implementation of the <see cref="IEmailService"/> with no templating support.
    /// </summary>
    public class EmailServiceSmtp : IEmailService
    {
        /// <summary>
        /// SMTP Settings.
        /// </summary>
        protected EmailServiceSettings Settings { get; }

        /// <summary>
        /// Cconstructs the service.
        /// </summary>
        /// <param name="settings">The SMTP settings to use.</param>
        public EmailServiceSmtp(EmailServiceSettings settings) => Settings = settings ?? throw new ArgumentNullException(nameof(settings));

        /// <inheritdoc/>
        public async Task SendAsync(string[] recipients, string subject, string body, FileAttachment[] attachments = null) => await SendAsync<object>(recipients, subject, body, null, null);

        /// <inheritdoc/>
        public async Task SendAsync<TModel>(string[] recipients, string subject, string body, string template, TModel data, FileAttachment[] attachments = null) where TModel : class {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(Settings.SenderName, Settings.Sender));
            message.To.AddRange(recipients.Select(recipient => InternetAddress.Parse(recipient)));
            if (!string.IsNullOrWhiteSpace(Settings.BccRecipients)) {
                var bccRecipients = Settings.BccRecipients.Split(',', ';');
                message.Bcc.AddRange(bccRecipients.Select(recipient => InternetAddress.Parse(recipient)));
            }
            message.Body = new TextPart(TextFormat.Html) {
                Text = body
            };
            message.Subject = subject;
            using (var client = new SmtpClient()) {
                // If UseSSL = true then you need to provide certificate in order to send the email, or else you get security exception.
                var useSSL = Settings.UseSSL && client.ClientCertificates != null && client.ClientCertificates.Count > 0;
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS).
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                // Since we don't have an OAuth2 token, disable the XOAUTH2 authentication mechanism.
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                // https://www.stevejgordon.co.uk/how-to-send-emails-in-asp-net-core-1-0
                // https://portal.smartertools.com/kb/a2862/smtp-settings-for-outlook365-and-gmail.aspx
                // Only needed if the SMTP server requires authentication.
                await client.ConnectAsync(Settings.SmtpHost, Settings.SmtpPort, SecureSocketOptions.Auto);
                if (!string.IsNullOrEmpty(Settings.Username)) {
                    client.Authenticate(Settings.Username, Settings.Password);
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
