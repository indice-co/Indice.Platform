using System;
using System.Collections.Generic;

namespace Indice.Configuration
{
    // Note: I do not like this implementation. Have to find a better way.
    public class EmailMessageBuilder<TModel> : EmailMessageBuilder where TModel : class
    {
        public TModel Data { get; set; }

        public new EmailMessageBuilder<TModel> AddRecipients(params string[] recipients) {
            if (recipients?.Length == 0) {
                throw new ArgumentException("One or more recipients must be declared for the message.", nameof(recipients));
            }
            foreach (var recipient in recipients) {
                Recipients.Add(recipient);
            }
            return this;
        }

        public new EmailMessageBuilder<TModel> AddSubject(string subject) {
            if (string.IsNullOrEmpty(subject)) {
                throw new ArgumentException("A subject for the message cannot be null or empty", nameof(subject));
            }
            Subject = subject;
            return this;
        }

        public new EmailMessageBuilder<TModel> AddBody(string body) {
            if (string.IsNullOrEmpty(body)) {
                throw new ArgumentException("A body for the message cannot be null or empty", nameof(body));
            }
            Body = body;
            return this;
        }

        public EmailMessageBuilder<TModel> AddData(TModel data) {
            Data = data;
            return this;
        }

        public new EmailMessageBuilder<TModel> AddTemplate(string template) {
            if (string.IsNullOrEmpty(template)) {
                throw new ArgumentException("A template name cannot be null or empty", nameof(template));
            }
            Template = template;
            return this;
        }

        public new EmailMessage<TModel> Build() => new EmailMessage<TModel>(Recipients, Subject, Body, Template, Data);
    }

    public class EmailMessageBuilder
    {
        public List<string> Recipients { get; set; } = new List<string>();
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Template { get; set; } = "Email";

        public EmailMessageBuilder AddRecipients(params string[] recipients) {
            if (recipients?.Length == 0) {
                throw new ArgumentException("One or more recipients must be declared for the message.", nameof(recipients));
            }
            foreach (var recipient in recipients) {
                Recipients.Add(recipient);
            }
            return this;
        }

        public EmailMessageBuilder AddSubject(string subject) {
            if (string.IsNullOrEmpty(subject)) {
                throw new ArgumentException("A subject for the message cannot be null or empty", nameof(subject));
            }
            Subject = subject;
            return this;
        }

        public EmailMessageBuilder AddBody(string body) {
            if (string.IsNullOrEmpty(body)) {
                throw new ArgumentException("A body for the message cannot be null or empty", nameof(body));
            }
            Body = body;
            return this;
        }

        public EmailMessageBuilder AddTemplate(string template) {
            if (string.IsNullOrEmpty(template)) {
                throw new ArgumentException("A template name cannot be null or empty", nameof(template));
            }
            Template = template;
            return this;
        }

        public EmailMessage Build() => new EmailMessage(Recipients, Subject, Body, Template);
    }
}
