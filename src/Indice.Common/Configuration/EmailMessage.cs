using System;
using System.Collections.Generic;
using System.IO;

namespace Indice.Configuration
{
    public class EmailMessage<TModel> : EmailMessage where TModel : class
    {
        public EmailMessage(IList<string> recipients, string subject, string body, string template, TModel data, IList<FileAttachment> attachments) 
            : base(recipients, subject, body, template, attachments) => Data = data;

        public TModel Data { get; }
    }

    public class EmailMessage
    {
        public EmailMessage(IList<string> recipients, string subject, string body, string template, IList<FileAttachment> attachments) {
            Recipients = recipients ?? throw new ArgumentNullException(nameof(recipients));
            Subject = subject ?? throw new ArgumentNullException(nameof(subject));
            Body = body ?? throw new ArgumentNullException(nameof(body));
            Template = template;
            Attachments = attachments;
        }

        public IList<string> Recipients { get; } = new List<string>();
        public string Subject { get; }
        public string Body { get; }
        public string Template { get; }
        public IList<FileAttachment> Attachments { get; set; }
    }

    public class FileAttachment
    {
        public FileAttachment(string fileName, Stream data) {
            FileName = fileName;
            using (var memoryStream = new MemoryStream()) {
                data.CopyTo(memoryStream);
                Data = memoryStream.ToArray();
            }
        }

        public FileAttachment(string fileName, byte[] data) {
            FileName = fileName;
            Data = data;
        }

        public byte[] Data { get; set; }
        public string FileName { get; set; }
    }
}
