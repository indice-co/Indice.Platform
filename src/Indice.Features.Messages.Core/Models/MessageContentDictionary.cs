namespace Indice.Features.Messages.Core.Models
{
    /// <summary>The content of a message.</summary>
    public class MessageContentDictionary : Dictionary<string, MessageContent>
    {
        /// <summary>Creates a new instance of <see cref="MessageContentDictionary"/> using the <see cref="StringComparer.OrdinalIgnoreCase"/> equality comparer.</summary>
        public MessageContentDictionary() : base(StringComparer.OrdinalIgnoreCase) { }

        /// <summary>Creates a new instance of <see cref="MessageContentDictionary"/> using the <see cref="StringComparer.OrdinalIgnoreCase"/> equality comparer.</summary>
        /// <param name="content">The initial content.</param>
        public MessageContentDictionary(Dictionary<string, MessageContent> content) : base(content, StringComparer.OrdinalIgnoreCase) { }

        /// <summary>Creates a new instance of <see cref="MessageContentDictionary"/> using the <see cref="StringComparer.OrdinalIgnoreCase"/> equality comparer.</summary>
        /// <param name="content">The initial content.</param>
        public MessageContentDictionary(IEnumerable<KeyValuePair<string, MessageContent>> content) : base(content, StringComparer.OrdinalIgnoreCase) { }

        /// <summary>Creates a new instance of <see cref="MessageContentDictionary"/> using the <see cref="StringComparer.OrdinalIgnoreCase"/> equality comparer.</summary>
        /// <param name="content">The initial content.</param>
        public MessageContentDictionary(Dictionary<MessageChannelKind, MessageContent> content) : base(content.Select(x => new KeyValuePair<string, MessageContent>(x.Key.ToString(), x.Value)), StringComparer.OrdinalIgnoreCase) { }
    }
}
