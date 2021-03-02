using System;
using System.Collections.Generic;
using System.Linq;

namespace Indice.Services
{
    
    /// <summary>
    /// A convinient builder to construct an instance of <see cref="EmailMessage"/>.
    /// </summary>
    public class EmailMessageBuilder {
        /// <summary>
        /// The email addresses of the recipients.
        /// </summary>
        public IList<string> Recipients { get; set; } = new List<string>();

        /// <summary>
        /// The subject of the message.
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// The body of the message.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// The template used to render the email. Defaults to 'Email'.
        /// </summary>
        public string Template { get; set; } = "Email";
        /// <summary>
        /// Data that are passed to the email template.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Optional attachments contained in the message.
        /// </summary>
        public IList<FileAttachment> Attachments { get; set; } = new List<FileAttachment>();
    }

    /// <summary>
    /// <see cref="EmailMessageBuilder" /> extensions
    /// </summary>
    public static class EmailMessageBuilderExtensions
    {

        /// <summary>
        /// Adds one or more recipients to the message.
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="recipients">The email addresses of the recipients.</param>
        /// <returns></returns>
        public static EmailMessageBuilder To(this EmailMessageBuilder builder, params string[] recipients) {
            if (recipients?.Length == 0) {
                throw new ArgumentException("One or more recipients must be declared for the message.", nameof(recipients));
            }
            foreach (var recipient in recipients.Distinct()) {
                builder.Recipients.Add(recipient);
            }
            return builder;
        }

        /// <summary>
        /// Defines the subject of the message.
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="subject">The subject of the message.</param>
        /// <returns></returns>
        public static EmailMessageBuilder WithSubject(this EmailMessageBuilder builder, string subject) {
            if (string.IsNullOrEmpty(subject)) {
                throw new ArgumentException("A subject for the message cannot be null or empty", nameof(subject));
            }
            builder.Subject = subject;
            return builder;
        }

        /// <summary>
        /// Defines the body of the message.
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="body">The body of the message.</param>
        /// <returns></returns>
        public static EmailMessageBuilder WithBody(this EmailMessageBuilder builder, string body) {
            if (string.IsNullOrEmpty(body)) {
                throw new ArgumentException("A body for the message cannot be null or empty", nameof(body));
            }
            builder.Body = body;
            return builder;
        }

        /// <summary>
        /// Defines the template used to render the email. Defaults to 'Email'. It has to be a Razor view, discoverable by the Razor Engine. (ex. Located in Views -> Shared folder).
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="template">The template used to render the email. Defaults to 'Email'.</param>
        /// <returns></returns>
        public static EmailMessageBuilder UsingTemplate(this EmailMessageBuilder builder, string template) {
            if (string.IsNullOrEmpty(template)) {
                throw new ArgumentException("A template name cannot be null or empty", nameof(template));
            }
            builder.Template = template;
            return builder;
        }

        /// <summary>
        /// Adds one or more attachments to the message. Attachments length cannot exceed 20 MB.
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="attachments">Optional attachments contained in the message.</param>
        /// <returns></returns>
        public static EmailMessageBuilder WithAttachments(this EmailMessageBuilder builder, params FileAttachment[] attachments) {
            if (attachments?.Length == 0) {
                throw new ArgumentException("One or more attachments must be declared for the message.", nameof(attachments));
            }
            builder.Attachments = attachments;
            return builder;
        }

        /// <summary>
        /// Adds a model that is passed to the email template.
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="data">Data that are passed to the email template.</param>
        /// <returns></returns>
        public static EmailMessageBuilder WithData<TModel>(this EmailMessageBuilder builder, TModel data) where TModel : class {
            builder.Data = data;
            return builder;
        }

        /// <summary>
        /// Returns the <see cref="EmailMessage"/> instance made by the builder.
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <returns></returns>
        public static EmailMessage Build(this EmailMessageBuilder builder) => 
            new EmailMessage(builder.Recipients, builder.Subject, builder.Body, builder.Template, builder.Data, builder.Attachments);
    }
}
