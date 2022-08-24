namespace Indice.Features.Messages.Core.Models.Requests
{
    /// <summary>The request model used to update an existing template.</summary>
    public class UpdateTemplateRequest
    {
        private Dictionary<string, MessageContent> _content;

        /// <summary>The name of the template.</summary>
        public string Name { get; set; }
        /// <summary>The content of the template.</summary>
        public Dictionary<string, MessageContent> Content {
            get { return _content; }
            set { _content = value is null ? new Dictionary<string, MessageContent>(StringComparer.OrdinalIgnoreCase) : value; }
        }
    }
}
