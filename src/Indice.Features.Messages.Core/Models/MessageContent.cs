namespace Indice.Features.Messages.Core.Models
{
    /// <summary>
    /// Encapsulates the message content for a given <see cref="MessageChannelKind"/>
    /// </summary>
    public class MessageContent
    {
        /// <summary>
        /// Creates a new instance of <see cref="MessageContent"/>.
        /// </summary>
        public MessageContent() { }

        /// <summary>
        /// Creates a new instance of <see cref="MessageContent"/>.
        /// </summary>
        /// <param name="title">The title of the message.</param>
        /// <param name="body">The body of the message.</param>
        public MessageContent(string title, string body) {
            Title = title;
            Body = body;
        }

        /// <summary>
        /// The title of the message.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The body of the message.
        /// </summary>
        public string Body { get; set; }
    }
}
