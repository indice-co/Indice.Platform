namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// The case type request model.
    /// </summary>
    public class CaseTypeRequest
    {
        /// <summary>
        /// The Id of the case type.
        /// </summary>
        public Guid? Id { get; set; }
        
        /// <summary>
        /// The Code of the case type.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The Title of the case type.
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

        /// <summary>
        /// The Data Schema of the case type
        /// </summary>
        public string DataSchema { get; set; }

        /// <summary>
        /// the Layout of the case type
        /// </summary>
        public string Layout { get; set; }

        /// <summary>
        /// The Translation for the case type
        /// </summary>
        public string Translations { get; set; }

        /// <summary>
        /// The Translation for the layout
        /// </summary>
        public string LayoutTranslations { get; set; }

        /// <summary>
        /// The list of checkpoints for the case type
        /// </summary>
        public IEnumerable<CheckpointTypeDetails> CheckpointTypes { get; set; }

        /// <summary>
        /// The case type tags.
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        /// The case type configuration.
        /// </summary>
        public string Config { get; set; }

        /// <summary>
        /// The allowed Roles that can create a new Case
        /// </summary>
        public string CanCreateRoles { get; set; }
    }
}
