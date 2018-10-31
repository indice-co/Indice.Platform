using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using MailKit.Security;
using MailKit.Net.Smtp;
using MimeKit;
namespace Indice.Services
{
    /// <summary>
    /// Simple smtp implementation of the <see cref="IEmailService"/> with no templating support.
    /// </summary>
    public class EmailServiceSmtp : IEmailService
    {
        /// <summary>
        /// Smtp Settings 
        /// </summary>
        protected EmailServiceSettings Settings { get; }

        /// <summary>
        /// constructs the service.
        /// </summary>
        /// <param name="settings"></param>
        public EmailServiceSmtp(EmailServiceSettings settings) {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Send email.
        /// </summary>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public async Task SendAsync(string[] recipients, string subject, string body) => await SendAsync<object>(recipients, subject, body, null, null);

        /// <summary>
        /// Send email.
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="template"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SendAsync<TModel>(string[] recipients, string subject, string body, string template, TModel data) where TModel : class {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(Settings.SenderName, Settings.Sender));
            message.To.AddRange(recipients.Select(recipient => InternetAddress.Parse(recipient)));
            message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };
            message.Subject = subject;

            using (var client = new MailKit.Net.Smtp.SmtpClient()) {
                // gko: If UseSSL = true then you need to provide certificate in order to send the email. Or else you get security exception.
                var useSSL = Settings.UseSSL && client.ClientCertificates != null && client.ClientCertificates.Count > 0;
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS).
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                // Note: since we don't have an OAuth2 token, disable the XOAUTH2 authentication mechanism.
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                // https://www.stevejgordon.co.uk/how-to-send-emails-in-asp-net-core-1-0
                // https://portal.smartertools.com/kb/a2862/smtp-settings-for-outlook365-and-gmail.aspx
                // Note: only needed if the SMTP server requires authentication.
                await client.ConnectAsync(Settings.SmtpHost, Settings.SmtpPort, SecureSocketOptions.Auto);

                if (!string.IsNullOrEmpty(Settings.Username)) {
                    client.Authenticate(Settings.Username, Settings.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}
