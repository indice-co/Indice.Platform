namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// The Translation of the case type.
    /// </summary>
    public class CaseTypeTranslation
    {
        /// <summary>
        /// The title of the case type.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The case type description.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The case type category.
        /// </summary>
        public string Category { get; set; }
    }
}