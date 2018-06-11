using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Services
{
    /// <summary>
    /// settings used to bootstrap email service clients
    /// </summary>
    public class EmailServiceSettings
    {
        public static readonly string Name = "Email";
        public string Sender { get; set; }
        public string SenderName { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public bool UseSSL { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Abstraction for sending email through different providers and implementations. SMTP, SparkPost, Mailchimp etc.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email.
        /// </summary>
        /// <param name="recipients">The recipients of the email message.</param>
        /// <param name="subject">The subject of the email message.</param>
        /// <param name="body">The body of the email message.</param>
        /// <returns></returns>
        Task SendAsync(string[] recipients, string subject, string body);

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
        Task SendAsync<TModel>(string[] recipients, string subject, string body, string template, TModel data) where TModel : class;
    }
}
