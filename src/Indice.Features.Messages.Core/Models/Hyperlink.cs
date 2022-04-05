namespace Indice.Features.Messages.Core.Models
{
    /// <summary>
    /// Models a hyperlink.
    /// </summary>
    public class Hyperlink
    {
        /// <summary>
        /// Defines the hyperlink text.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Defines the hyperlink URL.
        /// </summary>
        public string Href { get; set; }
    }
}
