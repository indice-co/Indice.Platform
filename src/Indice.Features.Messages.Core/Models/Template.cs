namespace Indice.Features.Messages.Core.Models
{
    /// <summary>Models a template.</summary>
    public class Template : TemplateBase
    {
        /// <summary>The content of the template.</summary>
        public Dictionary<string, MessageContent> Content { get; set; } = new Dictionary<string, MessageContent>(StringComparer.OrdinalIgnoreCase);
    }
}
