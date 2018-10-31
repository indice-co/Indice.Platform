using System;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using MimeKit;

namespace Indice.Services
{
    /// <summary>
    /// Razor enabled smtp <see cref="IEmailService"/>
    /// </summary>
    public class EmailServiceSmtpRazor : EmailServiceRazorBase
    {
        private readonly EmailServiceSettings _settings;

        /// <summary>
        /// constructs the service
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="viewEngine"></param>
        /// <param name="tempDataProvider"></param>
        /// <param name="httpContextAccessor"></param>
        public EmailServiceSmtpRazor(EmailServiceSettings settings, ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider, IHttpContextAccessor httpContextAccessor)
            : base(viewEngine, tempDataProvider, httpContextAccessor) {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Sends an email.
        /// </summary>
        /// <typeparam name="TModel">The type of the <paramref name="data"/> that will be applied to the template.</typeparam>
        /// <param name="recipients">The recipients of the email message.</param>
        /// <param name="subject">The subject of the email message.</param>
        /// <param name="body">The body of the email message.</param>
        /// <param name="template">The template of the email message.</param>
        /// <param name="data">The data model that contains information to render in the email message.</param>
        /// <returns></returns>
        public override async Task SendAsync<TModel>(string[] recipients, string subject, string body, string template, TModel data) {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.Sender));
            message.To.AddRange(recipients.Select(recipient => InternetAddress.Parse(recipient)));
            message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = await GetHtmlAsync<TModel>(body, subject, template.ToString(), data) };
            message.Subject = subject;

            using (var client = new MailKit.Net.Smtp.SmtpClient()) {
                // gko: If UseSSL = true then you need to provide certificate in order to send the email. Or else you get security exception.
                var useSSL = _settings.UseSSL && client.ClientCertificates != null && client.ClientCertificates.Count > 0;
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS).
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                // Note: since we don't have an OAuth2 token, disable the XOAUTH2 authentication mechanism.
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                // https://www.stevejgordon.co.uk/how-to-send-emails-in-asp-net-core-1-0
                // https://portal.smartertools.com/kb/a2862/smtp-settings-for-outlook365-and-gmail.aspx
                // Note: only needed if the SMTP server requires authentication.
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.Auto);

                if (!string.IsNullOrEmpty(_settings.Username)) {
                    client.Authenticate(_settings.Username, _settings.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}
