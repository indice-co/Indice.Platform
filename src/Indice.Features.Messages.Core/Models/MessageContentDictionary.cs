namespace Indice.Features.Messages.Core.Models
{
    /// <summary></summary>
    public class MessageContentDictionary : Dictionary<string, MessageContent>
    {
        /// <summary></summary>
        public MessageContentDictionary() : base(StringComparer.OrdinalIgnoreCase) { }

        /// <summary></summary>
        public MessageContentDictionary(Dictionary<string, MessageContent> d) : base(d, StringComparer.OrdinalIgnoreCase) { }

        /// <summary></summary>
        public MessageContentDictionary(IEnumerable<KeyValuePair<string, MessageContent>> d) : base(d, StringComparer.OrdinalIgnoreCase) { }

        /// <summary></summary>
        public MessageContentDictionary(Dictionary<MessageChannelKind, MessageContent> d) : base(d.Select(x => new KeyValuePair<string, MessageContent>(x.Key.ToString(), x.Value)), StringComparer.OrdinalIgnoreCase) { }
    }
}
