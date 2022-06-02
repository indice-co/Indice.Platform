using System.Dynamic;

namespace Indice.Features.Messages.Core.Models
{
    /// <summary></summary>
    public class PreviewItem
    {
        /// <summary>A correllation code.</summary>
        public string Code { get; set; }
        /// <summary>The template text.</summary>
        public string Text { get; set; }
        /// <summary>The data to supply in the template.</summary>
        public ExpandoObject Data { get; set; }
    }

    /// <summary></summary>
    public class PreviewItemResult
    {
        /// <summary>A correllation code.</summary>
        public string Code { get; set; }
        /// <summary></summary>
        public string Text { get; set; }
    }
}
