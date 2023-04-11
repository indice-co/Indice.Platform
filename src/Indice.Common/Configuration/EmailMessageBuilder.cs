namespace Indice.Services;

/// <summary>A convenient builder to construct an instance of <see cref="EmailMessage"/>.</summary>
public class EmailMessageBuilder
{
    /// <summary>The email addresses of the recipients.</summary>
    internal IList<string> Recipients { get; set; } = new List<string>();
    /// <summary>The representation of an email address in the form field.</summary>
    /// <remarks>Defaults to the configuration values <strong>Email:Sender</strong> and <strong>Email:SenderName</strong>.</remarks>
    internal EmailSender Sender { get; set; }
    /// <summary>The subject of the message.</summary>
    internal string Subject { get; set; }
    /// <summary>The body of the message.</summary>
    internal string Body { get; set; }
    /// <summary>Optional attachments contained in the message.</summary>
    internal IList<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
    /// <summary>The template used to render the email.</summary>
    internal string Template { get; set; }
    /// <summary>Data that are passed to the email template.</summary>
    internal object? Data { get; set; }
}

/// <summary><see cref="EmailMessageBuilder" /> extensions.</summary>
public static class EmailMessageBuilderExtensions
{
    /// <summary>Configures the sender of the message, overriding the default configuration</summary>
    /// <param name="builder">The builder.</param>
    /// <param name="address">Sender address.</param>
    /// <param name="displayName">Sender display name.</param>
    /// <returns>The builder.</returns>
    /// <remarks>Take caution. The value must be valid according to the sending domains configured with the corresponding API key.</remarks>
    public static EmailMessageBuilder From(this EmailMessageBuilder builder, string address, string displayName = null) {
        if (string.IsNullOrEmpty(address)) {
            throw new ArgumentNullException("The email address must be provided.", nameof(address));
        }
        builder.Sender = new EmailSender(address, displayName);
        return builder;
    }

    /// <summary>Adds one or more recipients to the message.</summary>
    /// <param name="builder">The builder.</param>
    /// <param name="recipients">The email addresses of the recipients.</param>
    /// <returns>The builder.</returns>
    public static EmailMessageBuilder To(this EmailMessageBuilder builder, params string[] recipients) {
        if (recipients?.Length == 0) {
            throw new ArgumentException("One or more recipients must be declared for the message.", nameof(recipients));
        }
        foreach (var recipient in recipients.Distinct()) {
            builder.Recipients.Add(recipient);
        }
        return builder;
    }

    /// <summary>Defines the subject of the message.</summary>
    /// <param name="builder">The builder.</param>
    /// <param name="subject">The subject of the message.</param>
    /// <returns>The builder.</returns>
    public static EmailMessageBuilder WithSubject(this EmailMessageBuilder builder, string subject) {
        if (string.IsNullOrEmpty(subject)) {
            throw new ArgumentException("A subject for the message cannot be null or empty", nameof(subject));
        }
        builder.Subject = subject;
        return builder;
    }

    /// <summary>Defines the body of the message.</summary>
    /// <param name="builder">The builder.</param>
    /// <param name="body">The body of the message.</param>
    /// <returns>The builder.</returns>
    public static EmailMessageBuilder WithBody(this EmailMessageBuilder builder, string body) {
        if (string.IsNullOrEmpty(body)) {
            throw new ArgumentException("A body for the message cannot be null or empty", nameof(body));
        }
        builder.Body = body;
        return builder;
    }

    /// <summary>Adds one or more attachments to the message. Attachments length cannot exceed 20 MB.</summary>
    /// <param name="builder">The builder.</param>
    /// <param name="attachments">Optional attachments contained in the message.</param>
    /// <returns>The builder.</returns>
    public static EmailMessageBuilder WithAttachments(this EmailMessageBuilder builder, params EmailAttachment[] attachments) {
        if (attachments?.Length == 0) {
            throw new ArgumentException("One or more attachments must be declared for the message.", nameof(attachments));
        }
        foreach (var attachment in attachments) {
            builder.Attachments.Add(attachment);
        }
        return builder;
    }

    /// <summary>
    /// Defines the template used to render the email. If set, it takes precedence over <see cref="WithBody(EmailMessageBuilder, string)"/> method. 
    /// It has to be a Razor view, discoverable by the Razor Engine. (ex. Located in Views -> Shared folder).
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="template">The template used to render the email. Defaults to 'Email'.</param>
    /// <returns>The builder.</returns>
    public static EmailMessageBuilder UsingTemplate(this EmailMessageBuilder builder, string template) {
        if (string.IsNullOrEmpty(template)) {
            throw new ArgumentException("A template name cannot be null or empty", nameof(template));
        }
        builder.Template = template;
        return builder;
    }

    /// <summary>Adds a model that is passed to the email template.</summary>
    /// <param name="builder">The builder.</param>
    /// <param name="data">Data that are passed to the email template.</param>
    /// <returns>The builder.</returns>
    public static EmailMessageBuilder WithData<TModel>(this EmailMessageBuilder builder, TModel data) where TModel : class {
        builder.Data = data;
        return builder;
    }

    /// <summary>Returns the <see cref="EmailMessage"/> instance made by the builder.</summary>
    /// <param name="builder">The builder.</param>
    /// <returns>The configured <see cref="EmailMessage"/>.</returns>
    public static EmailMessage Build(this EmailMessageBuilder builder) =>
        new(builder.Recipients, builder.Subject, builder.Body, builder.Template, builder.Data, builder.Attachments);
}
