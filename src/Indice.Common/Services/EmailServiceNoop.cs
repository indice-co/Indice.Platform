using System.Diagnostics;

namespace Indice.Services;

/// <summary>A default implementation for <see cref="IEmailService"/> that does nothing.</summary>
public class EmailServiceNoop : IEmailService
{
    /// <inheritdoc/>
    public IHtmlRenderingEngine HtmlRenderingEngine { get; } = null;

    /// <inheritdoc/>
    public Task SendAsync(string[] recipients, string subject, string body, EmailAttachment[] attachments = null, EmailSender from = null) {
        foreach (var recipient in recipients) {
            Debug.WriteLine($"Email:\n\t\t{recipient}/{subject}\n\n\t\t{body}");
        }
        return Task.CompletedTask;
    }
}
