namespace Indice.Features.Messages.Core.Models
{
    /// <summary>Models a template.</summary>
    public class Template : TemplateBase
    {
        /// <summary>The content of the template.</summary>
        public Dictionary<string, MessageContent> Content { get; set; } = new Dictionary<string, MessageContent>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Models a template when retrieved on a list.</summary>
    public class TemplateListItem : TemplateBase
    {
        /// <summary>The channels that this template supports.</summary>
        public MessageChannelKind Channels { get; set; }
    }

    /// <summary>Models a template's basic information.</summary>
    public class TemplateBase
    {
        /// <summary>The unique id of the template.</summary>
        public Guid Id { get; set; }
        /// <summary>The name of the template.</summary>
        public string Name { get; set; }
        /// <summary>When the template was created.</summary>
        public DateTimeOffset CreatedAt { get; set; }
    }
}
