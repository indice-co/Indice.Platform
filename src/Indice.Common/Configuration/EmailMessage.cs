using System;
using System.Collections.Generic;
using System.IO;

namespace Indice.Services
{
    /// <summary>Models the data that are sent in an email message.</summary>
    public class EmailMessage
    {
        /// <summary>Constructs a new <see cref="EmailMessage"/>.</summary>
        /// <param name="recipients">The email addresses of the recipients.</param>
        /// <param name="subject">The subject of the message.</param>
        /// <param name="body">The body of the message.</param>
        /// <param name="template">The template used to render the email. Defaults to 'Email'.</param>
        /// <param name="data">Data that are passed to the email template.</param>
        /// <param name="attachments">Optional attachments contained in the message.</param>
        public EmailMessage(IList<string> recipients, string subject, string body, string template, object data, IList<EmailAttachment> attachments) {
            Recipients = recipients ?? throw new ArgumentNullException(nameof(recipients));
            Subject = subject ?? throw new ArgumentNullException(nameof(subject));
            if (string.IsNullOrEmpty(body) && string.IsNullOrEmpty(template)) {
                throw new ArgumentNullException($"{nameof(body)} if no template is used then the body parameter cannot be null");
            }
            Body = body;
            Template = template;
            Attachments = attachments;
            Data = data;
        }

        /// <summary>The email addresses of the recipients.</summary>
        internal IList<string> Recipients { get; } = new List<string>();
        /// <summary>The subject of the message.</summary>
        internal string Subject { get; }
        /// <summary>The body of the message.</summary>
        internal string Body { get; set; }
        /// <summary>The template used to render the email.</summary>
        internal string Template { get; }
        /// <summary>Data that are passed to the email template.</summary>
        internal object Data { get; }
        /// <summary>Optional attachments contained in the message.</summary>
        internal IList<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
    }

    /// <summary>Models the optional attachment of an email message.</summary>
    public class EmailAttachment
    {
        /// <summary>Constructs a new <see cref="EmailAttachment"/>.</summary>
        /// <param name="fileName">The name of the attachment.</param>
        /// <param name="data">The attachment data as a <see cref="Stream"/>.</param>
        public EmailAttachment(string fileName, Stream data) {
            FileName = fileName;
            using (var memoryStream = new MemoryStream()) {
                data.CopyTo(memoryStream);
                Data = memoryStream.ToArray();
            }
        }

        /// <summary>Constructs a new <see cref="EmailAttachment"/>.</summary>
        /// <param name="fileName">The name of the attachment.</param>
        /// <param name="data">The attachment data as an array of bytes.</param>
        public EmailAttachment(string fileName, byte[] data) {
            FileName = fileName;
            Data = data;
        }

        /// <summary>The attachment data as an array of bytes.</summary>
        public byte[] Data { get; set; }
        /// <summary>The name of the attachment.</summary>
        public string FileName { get; set; }
    }
}
