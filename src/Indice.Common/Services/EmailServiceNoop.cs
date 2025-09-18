using System.Diagnostics;

namespace Indice.Services;

/// <summary>A default implementation for <see cref="IEmailService"/> that does nothing.</summary>
public class EmailServiceNoop : IEmailService
{
    /// <summary>Represents the name of the Brevo service as a constant string value.</summary>
    public const string ServiceName = "None";
    /// <inheritdoc/>
    public IHtmlRenderingEngine? HtmlRenderingEngine { get; } = null;


    /// <inheritdoc/>
    public EmailProvider Provider { get; } = new EmailProvider(ServiceName, new EmailSender(string.Empty, "Unknown"));

    /// <inheritdoc/>
    public Task<SendReceipt> SendAsync(string[] recipients, string subject, string? body, EmailAttachment[]? attachments = null, EmailSender? from = null) {
        foreach (var recipient in recipients) {
            Debug.WriteLine($"Email:\n\t\t{recipient}/{subject}\n\n\t\t{body}");
        }
        return Task.FromResult(new SendReceipt(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow));
    }
}
