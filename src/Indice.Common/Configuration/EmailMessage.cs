using System.Collections.Generic;
using System.IO;

namespace Indice.Configuration
{
    public class EmailMessage<TModel> : EmailMessage where TModel : class
    {
        public EmailMessage(IList<string> recipients, string subject, string body, string template, TModel data) : base(recipients, subject, body, template) {
            Data = data;
        }

        public TModel Data { get; }
    }

    public class EmailMessage
    {
        public EmailMessage(IList<string> recipients, string subject, string body, string template) {
            Recipients = recipients;
            Subject = subject;
            Body = body;
            Template = template;
        }

        public IList<string> Recipients { get; } = new List<string>();
        public string Subject { get; }
        public string Body { get; }
        public string Template { get; }
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
