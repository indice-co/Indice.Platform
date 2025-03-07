using System.Diagnostics;
using Indice.Configuration;

namespace Indice.Services;

/// <summary>The representation of an email address in the form field.</summary>
/// <remarks>Defaults to the configuration values <strong>Email:Sender</strong> and <strong>Email:SenderName</strong>.</remarks>
public class EmailSender
{
    /// <summary>Creates a new instance of <see cref="EmailSender"/>.</summary>
    /// <param name="address">Email address.</param>
    /// <param name="displayName">Display name.</param>
    public EmailSender(string address, string? displayName) {
        Address = address;
        DisplayName = displayName;
    }

    /// <summary>Email address.</summary>
    public string Address { get; }
    /// <summary>Display name.</summary>
    public string? DisplayName { get; }
    /// <summary>Checks for address existence.</summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Address);
    /// <inheritdoc/>
    public override string ToString() => IsEmpty ? base.ToString()! : $"{DisplayName} <{Address}>";
}

/// <summary>Abstraction for sending email through different providers and implementations. SMTP, SparkPost, Mailchimp etc.</summary>
public interface IEmailService
{
    /// <summary>This is an abstraction for the rendering engine.</summary>
    public IHtmlRenderingEngine? HtmlRenderingEngine { get; }
    /// <summary>Sends an email.</summary>
    /// <param name="recipients">The recipients of the email message.</param>
    /// <param name="subject">The subject of the email message.</param>
    /// <param name="body">The body of the email message.</param>
    /// <param name="attachments">The files that will be attached in the email message.</param>
    /// <param name="from">Optional email address in the form field. Defaults to the configuration values <strong>Email:Sender</strong> and <strong>Email:SenderName</strong>.</param>    
    Task<SendReceipt> SendAsync(string[] recipients, string subject, string? body, EmailAttachment[]? attachments = null, EmailSender? from = null);
}

/// <summary>Exception for Email service failure.</summary>
public class EmailServiceException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="EmailServiceException"/> class.</summary>
    public EmailServiceException() { }

    /// <summary>Initializes a new instance of the <see cref="EmailServiceException"/> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public EmailServiceException(string? message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="EmailServiceException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public EmailServiceException(string? message, Exception? innerException) : base(message, innerException) { }
}

/// <summary>Service extensions for <see cref="IEmailService"/>.</summary>
public static class EmailServiceExtensions
{
    /// <summary>Send an email using a single recipient.</summary>
    /// <param name="emailService">Abstraction for sending email through different providers and implementations. SMTP, SparkPost, Mailchimp etc.</param>
    /// <param name="recipient">The recipient of the email message.</param>
    /// <param name="subject">The subject of the email message.</param>
    /// <param name="body">The body of the email message.</param>
    public static async Task<SendReceipt> SendAsync(this IEmailService emailService, string recipient, string subject, string body) =>
        await emailService.SendAsync([ recipient ], subject, body);

    /// <summary>Sends an email by using a fluent configuration.</summary>
    /// <param name="emailService">Abstraction for sending email through different providers and implementations. SMTP, SparkPost, Mailchimp etc.</param>
    /// <param name="configureMessage">The delegate that will be used to build the message.</param>
    public static async Task<SendReceipt> SendAsync(this IEmailService emailService, Action<EmailMessageBuilder> configureMessage) {
        if (configureMessage == null) {
            throw new ArgumentNullException(nameof(configureMessage));
        }
        var builder = new EmailMessageBuilder();
        configureMessage?.Invoke(builder);
        var message = builder.Build();
        if (!string.IsNullOrWhiteSpace(message.Template) && emailService.HtmlRenderingEngine is not null) {
            message.Body = await emailService.HtmlRenderingEngine.RenderAsync(message.Template!, message.Data);
        }
        return await emailService.SendAsync([.. message.Recipients], message.Subject, message.Body, [.. message.Attachments], message.Sender);
    }
}

/// <summary>Settings used to bootstrap email service clients.</summary>
public class EmailServiceSettings
{
    /// <summary>The configuration section name.</summary>
    public static readonly string Name = "Email";
    /// <summary>The default sender address (ex. no-reply@indice.gr).</summary>
    public string? Sender { get; set; }
    /// <summary>The default sender name (ex. INDICE OE).</summary>
    public string? SenderName { get; set; }
    /// <summary>The host of the SMTP server (i.e mail.indice.gr).</summary>
    public string? SmtpHost { get; set; }
    /// <summary>The port that the SMTP server is listening.</summary>
    public int SmtpPort { get; set; }
    /// <summary>Toggles between HTTP and HTTPS.</summary>
    public bool UseSSL { get; set; }
    /// <summary>The <see cref="Username"/> to use on the credentials that will be sent over to consume the SMTP service.</summary>
    public string? Username { get; set; }
    /// <summary>
    /// The <see cref="Password"/> to use on the credentials that will be sent over to consume the SMTP service. 
    /// This is optional in case we are inside a domain (SMTP relay).
    /// </summary>
    public string? Password { get; set; }
    /// <summary>Optional email addresses that are always added as blind carbon copy recipients.</summary>
    public string? BccRecipients { get; set; }
    /// <summary>Provides a way of specifying the SSL and/or TLS encryption that should be used for a connection.</summary>
    public SecureSocketOptions SecureSocket { get; set; } = SecureSocketOptions.Auto;
    /// <summary>Get or set whether connecting via SSL/TLS should check certificate revocation.</summary>
    public bool CheckCertificateRevocation { get; set; } = true;
}