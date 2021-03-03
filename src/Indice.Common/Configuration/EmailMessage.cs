using System;
using System.Collections.Generic;
using System.IO;

namespace Indice.Services
{
    /// <summary>
    /// Models the data that are sent in an email message.
    /// </summary>
    public class EmailMessage
    {
        /// <summary>
        /// Constructs a new <see cref="EmailMessage"/>.
        /// </summary>
        /// <param name="recipients">The email addresses of the recipients.</param>
        /// <param name="subject">The subject of the message.</param>
        /// <param name="body">The body of the message.</param>
        /// <param name="template">The template used to render the email. Defaults to 'Email'.</param>
        /// <param name="data">Data that are passed to the email template.</param>
        /// <param name="attachments">Optional attachments contained in the message.</param>
        public EmailMessage(IList<string> recipients, string subject, string body, string template, object data, IList<FileAttachment> attachments) {
            Recipients = recipients ?? throw new ArgumentNullException(nameof(recipients));
            Subject = subject ?? throw new ArgumentNullException(nameof(subject));
            Body = body ?? throw new ArgumentNullException(nameof(body));
            Template = template;
            Attachments = attachments;
            Data = data;
        }

        /// <summary>
        /// The email addresses of the recipients.
        /// </summary>
        public IList<string> Recipients { get; } = new List<string>();
        /// <summary>
        /// The subject of the message.
        /// </summary>
        public string Subject { get; }
        /// <summary>
        /// The body of the message.
        /// </summary>
        public string Body { get; }
        /// <summary>
        /// The template used to render the email. Defaults to 'Email'.
        /// </summary>
        public string Template { get; }
        /// <summary>
        /// Data that are passed to the email template.
        /// </summary>
        public object Data { get; }
        /// <summary>
        /// Optional attachments contained in the message.
        /// </summary>
        public IList<FileAttachment> Attachments { get; set; }
    }

    /// <summary>
    /// Models the optional attachment of an email message.
    /// </summary>
    public class FileAttachment
    {
        /// <summary>
        /// Constructs a new <see cref="FileAttachment"/>.
        /// </summary>
        /// <param name="fileName">The name of the attachment.</param>
        /// <param name="data">The attachment data as a <see cref="Stream"/>.</param>
        public FileAttachment(string fileName, Stream data) {
            FileName = fileName;
            using (var memoryStream = new MemoryStream()) {
                data.CopyTo(memoryStream);
                Data = memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Constructs a new <see cref="FileAttachment"/>.
        /// </summary>
        /// <param name="fileName">The name of the attachment.</param>
        /// <param name="data">The attachment data as an array of bytes.</param>
        public FileAttachment(string fileName, byte[] data) {
            FileName = fileName;
            Data = data;
        }

        /// <summary>
        /// The attachment data as an array of bytes.
        /// </summary>
        public byte[] Data { get; set; }
        /// <summary>
        /// The name of the attachment.
        /// </summary>
        public string FileName { get; set; }
    }
}
