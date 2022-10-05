using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Indice.Services
{
    /// <summary>A default implementation for <see cref="IEmailService"/> that does nothing.</summary>
    public class EmailServiceNoop : IEmailService
    {
        /// <summary>Creates a new instance of <see cref="EmailServiceNoop"/>.</summary>
        /// <param name="htmlRenderingEngine">This is an abstraction for the rendering engine.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EmailServiceNoop(IHtmlRenderingEngine htmlRenderingEngine) {
            HtmlRenderingEngine = htmlRenderingEngine ?? throw new ArgumentNullException(nameof(htmlRenderingEngine));
        }

        /// <inheritdoc/>
        public IHtmlRenderingEngine HtmlRenderingEngine { get; }

        /// <inheritdoc/>
        public Task SendAsync(string[] recipients, string subject, string body, EmailAttachment[] attachments = null) {
            foreach (var recipient in recipients) {
                Debug.WriteLine($"Email:\n\t\t{recipient}/{subject}\n\n\t\t{body}");
            }
            return Task.CompletedTask;
        }
    }
}
