using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Data.Models
{
    /// <summary>Template entity.</summary>
    public class DbTemplate
    {
        /// <summary>The unique id of the template.</summary>
        public Guid Id { get; set; }
        /// <summary>The name of the template.</summary>
        public string Name { get; set; }
        /// <summary>When the template was created.</summary>
        public DateTimeOffset CreatedAt { get; set; }
        /// <summary>The contents of the template.</summary>
        public Dictionary<string, MessageContent> Content { get; set; } = new Dictionary<string, MessageContent>(StringComparer.OrdinalIgnoreCase);
    }
}
