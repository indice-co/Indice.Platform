using System.Collections.Generic;
using System.IO;

namespace Indice.Configuration
{
    public class EmailMessage<TModel> : EmailMessage where TModel : class
    {
        public EmailMessage(List<string> recipients, string subject, string body, string template, TModel data) : base(recipients, subject, body, template) {
            Data = data;
        }

        public TModel Data { get; }
    }

    public class EmailMessage
    {
        public EmailMessage(List<string> recipients, string subject, string body, string template) {
            Recipients = recipients;
            Subject = subject;
            Body = body;
            Template = template;
        }

        public List<string> Recipients { get; } = new List<string>();
        public string Subject { get; }
        public string Body { get; }
        public string Template { get; }
    }

    public class FileAttachment
    {
        public FileAttachment(string fileName, Stream data) {
            FileName = fileName;
            Data = data;
        }

        public FileAttachment(string fileName, byte[] data) {
            FileName = fileName;
            Data = new MemoryStream(data);
        }

        public Stream Data { get; set; }
        public string FileName { get; set; }
    }
}
