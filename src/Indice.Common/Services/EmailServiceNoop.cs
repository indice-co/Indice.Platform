using System;
using System.Threading.Tasks;

namespace Indice.Services
{
    /// <summary>
    /// Noop implementation of <see cref="IEmailService"/>.
    /// </summary>
    public class EmailServiceNoop : IEmailService
    {
        /// <inheritdoc/>
        public Task SendAsync(string[] recipients, string subject, string body, EmailAttachment[] attachments = null) {
            foreach (var recipient in recipients) {
                Console.WriteLine($"Email:\n\t\t{recipient}/{subject}\n\n\t\t{body}");
            }
            return Task.CompletedTask;
        }
    }
}
