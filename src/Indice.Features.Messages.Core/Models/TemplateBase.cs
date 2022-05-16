namespace Indice.Features.Messages.Core.Models
{
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
