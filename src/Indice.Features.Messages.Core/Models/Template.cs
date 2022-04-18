namespace Indice.Features.Messages.Core.Models
{
    /// <summary>
    /// Models a template.
    /// </summary>
    public class Template
    {
        /// <summary>
        /// The unique id of the template.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The name of the template.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The content of the template.
        /// </summary>
        public Dictionary<string, MessageContent> Content { get; set; } = new Dictionary<string, MessageContent>(StringComparer.OrdinalIgnoreCase);
    }
}
