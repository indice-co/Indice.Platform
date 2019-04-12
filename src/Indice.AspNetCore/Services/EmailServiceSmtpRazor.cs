using System;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using MimeKit;
using MimeKit.Text;

namespace Indice.Services
{
    /// <summary>
    /// Razor enabled SMTP <see cref="IEmailService"/>.
    /// </summary>
    public class EmailServiceSmtpRazor : EmailServiceRazorBase
    {
        private readonly EmailServiceSettings _settings;

        /// <summary>
        /// Constructs the service
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="viewEngine">Represents an <see cref="IViewEngine"/> that delegates to one of a collection of view engines.></param>
        /// <param name="tempDataProvider">Defines the contract for temporary-data providers that store data that is viewed on the next request.</param>
        /// <param name="httpContextAccessor">Used to access the <see cref="HttpContext"/> through the <see cref="IHttpContextAccessor"/> interface and its default implementation <see cref="HttpContextAccessor"/>.</param>
        public EmailServiceSmtpRazor(EmailServiceSettings settings, ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider, IHttpContextAccessor httpContextAccessor)
            : base(viewEngine, tempDataProvider, httpContextAccessor) => _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        /// <inheritdoc/>
        public override async Task SendAsync<TModel>(string[] recipients, string subject, string body, string template, TModel data) {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.Sender));
            message.To.AddRange(recipients.Select(recipient => InternetAddress.Parse(recipient)));
            if (!string.IsNullOrWhiteSpace(_settings.BccRecipients)) {
                var bccRecipients = _settings.BccRecipients.Split(',', ';');
                message.Bcc.AddRange(bccRecipients.Select(recipient => InternetAddress.Parse(recipient)));
            }
            message.Body = new TextPart(TextFormat.Html) {
                Text = await GetHtmlAsync(body, subject, template.ToString(), data)
            };
            message.Subject = subject;
            using (var client = new SmtpClient()) {
                // If UseSSL = true then you need to provide certificate in order to send the email. Or else you get security exception.
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
    }
}
