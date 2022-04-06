namespace Indice.Features.Messages.Core.Models
{
    /// <summary>
    /// Encapsulates the message content for a given <see cref="MessageChannelKind"/>
    /// </summary>
    public class MessageContent
    {
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
