using System.Diagnostics;
using System.Threading.Tasks;

namespace Indice.Services
{
    /// <summary>
    /// A default implementation for <see cref="IEmailService"/> that does nothing.
    /// </summary>
    public class EmailServiceNoop : IEmailService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlRenderingEngine"></param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EmailServiceNoop(IHtmlRenderingEngine htmlRenderingEngine) {
            HtmlRenderingEngine = htmlRenderingEngine ?? throw new System.ArgumentNullException(nameof(htmlRenderingEngine));
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
