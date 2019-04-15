using System;
using System.Collections.Generic;
using System.Linq;

namespace Indice.Configuration
{
    /// <summary>
    /// A convinient builder to construct an instance of <see cref="EmailMessage{TModel}"/>.
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public class EmailMessageBuilder<TModel> : EmailMessageBuilder where TModel : class
    {
        /// <summary>
        /// Data that are paased to the email template.
        /// </summary>
        public TModel Data { get; set; }

        /// <summary>
        /// Adds one or more recipients to the message.
        /// </summary>
        /// <param name="recipients">The email addresses of the recipients.</param>
        /// <returns></returns>
        public new EmailMessageBuilder<TModel> To(params string[] recipients) {
            if (recipients?.Length == 0) {
                throw new ArgumentException("One or more recipients must be declared for the message.", nameof(recipients));
            }
            foreach (var recipient in recipients.Distinct()) {
                Recipients.Add(recipient);
            }
            return this;
        }

        /// <summary>
        /// Defines the subject of the message.
        /// </summary>
        /// <param name="subject">The subject of the message.</param>
        /// <returns></returns>
        public new EmailMessageBuilder<TModel> WithSubject(string subject) {
            if (string.IsNullOrEmpty(subject)) {
                throw new ArgumentException("A subject for the message cannot be null or empty", nameof(subject));
            }
            Subject = subject;
            return this;
        }

        /// <summary>
        /// Defines the body of the message.
        /// </summary>
        /// <param name="body">The body of the message.</param>
        /// <returns></returns>
        public new EmailMessageBuilder<TModel> WithBody(string body) {
            if (string.IsNullOrEmpty(body)) {
                throw new ArgumentException("A body for the message cannot be null or empty", nameof(body));
            }
            Body = body;
            return this;
        }

        /// <summary>
        /// Adds a model that is passed to the email template.
        /// </summary>
        /// <param name="data">Data that are paased to the email template.</param>
        /// <returns></returns>
        public EmailMessageBuilder<TModel> WithData(TModel data) {
            Data = data;
            return this;
        }

        /// <summary>
        /// Defines the template used to render the email. Defaults to 'Email'. It has to be a Razor view, discoverable by the Razor Engine. (ex. Located in Views -> Shared folder).
        /// </summary>
        /// <param name="template">The template used to render the email. Defaults to 'Email'.</param>
        /// <returns></returns>
        public new EmailMessageBuilder<TModel> UsingTemplate(string template) {
            if (string.IsNullOrEmpty(template)) {
                throw new ArgumentException("A template name cannot be null or empty", nameof(template));
            }
            Template = template;
            return this;
        }

        /// <summary>
        /// Adds one or more attachments to the message. Attachments length cannot exceed 20 MB.
        /// </summary>
        /// <param name="attachments">Optional attachments contained in the message.</param>
        /// <returns></returns>
        public new EmailMessageBuilder<TModel> WithAttachments(params FileAttachment[] attachments) {
            if (attachments?.Length == 0) {
                throw new ArgumentException("One or more attachments must be declared for the message.", nameof(attachments));
            }
            Attachments = attachments;
            return this;
        }

        /// <summary>
        /// Returns the <see cref="EmailMessage{TModel}"/> instance made by the builder.
        /// </summary>
        /// <returns></returns>
        public new EmailMessage<TModel> Build() => new EmailMessage<TModel>(Recipients, Subject, Body, Template, Data, Attachments);
    }

    /// <summary>
    /// A convinient builder to construct an instance of <see cref="EmailMessage"/>.
    /// </summary>
    public class EmailMessageBuilder
    {
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
        /// Optional attachments contained in the message.
        /// </summary>
        public IList<FileAttachment> Attachments { get; set; } = new List<FileAttachment>();

        /// <summary>
        /// Adds one or more recipients to the message.
        /// </summary>
        /// <param name="recipients">The email addresses of the recipients.</param>
        /// <returns></returns>
        public EmailMessageBuilder To(params string[] recipients) {
            if (recipients?.Length == 0) {
                throw new ArgumentException("One or more recipients must be declared for the message.", nameof(recipients));
            }
            foreach (var recipient in recipients.Distinct()) {
                Recipients.Add(recipient);
            }
            return this;
        }

        /// <summary>
        /// Defines the subject of the message.
        /// </summary>
        /// <param name="subject">The subject of the message.</param>
        /// <returns></returns>
        public EmailMessageBuilder WithSubject(string subject) {
            if (string.IsNullOrEmpty(subject)) {
                throw new ArgumentException("A subject for the message cannot be null or empty", nameof(subject));
            }
            Subject = subject;
            return this;
        }

        /// <summary>
        /// Defines the body of the message.
        /// </summary>
        /// <param name="body">The body of the message.</param>
        /// <returns></returns>
        public EmailMessageBuilder WithBody(string body) {
            if (string.IsNullOrEmpty(body)) {
                throw new ArgumentException("A body for the message cannot be null or empty", nameof(body));
            }
            Body = body;
            return this;
        }

        /// <summary>
        /// Defines the template used to render the email. Defaults to 'Email'. It has to be a Razor view, discoverable by the Razor Engine. (ex. Located in Views -> Shared folder).
        /// </summary>
        /// <param name="template">The template used to render the email. Defaults to 'Email'.</param>
        /// <returns></returns>
        public EmailMessageBuilder UsingTemplate(string template) {
            if (string.IsNullOrEmpty(template)) {
                throw new ArgumentException("A template name cannot be null or empty", nameof(template));
            }
            Template = template;
            return this;
        }

        /// <summary>
        /// Adds one or more attachments to the message. Attachments length cannot exceed 20 MB.
        /// </summary>
        /// <param name="attachments">Optional attachments contained in the message.</param>
        /// <returns></returns>
        public EmailMessageBuilder WithAttachments(params FileAttachment[] attachments) {
            if (attachments?.Length == 0) {
                throw new ArgumentException("One or more attachments must be declared for the message.", nameof(attachments));
            }
            Attachments = attachments;
            return this;
        }

        /// <summary>
        /// Returns the <see cref="EmailMessage"/> instance made by the builder.
        /// </summary>
        /// <returns></returns>
        public EmailMessage Build() => new EmailMessage(Recipients, Subject, Body, Template, Attachments);
    }
}
