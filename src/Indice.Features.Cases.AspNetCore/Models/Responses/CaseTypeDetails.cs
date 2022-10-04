using System;
using System.Collections.Generic;

namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// The case type details model.
    /// </summary>
    public class CaseTypeDetails
    {
        /// <summary>
        /// The Id of the case type.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The case type code.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// The case type title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The case type json schema.
        /// </summary>
        public string? DataSchema { get; set; }

        /// <summary>
        /// The layout for the data schema.
        /// </summary>
        public string? Layout { get; set; }

        /// <summary>
        /// The case type translations.
        /// </summary>
        public string? Translations { get; set; }

        /// <summary>
        /// The layout translations.
        /// </summary>
        public string? LayoutTranslations { get; set; }

        /// <summary>
        /// The case type tags.
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// The allowed Roles that can create a new Case
        /// </summary>
        public string? AllowedRolesForCaseCreation { get; set; }

        /// <summary>
        /// The checkpoints for this case type.
        /// </summary>
        public IEnumerable<CheckpointTypeDetails> CheckpointTypes { get; set; }
    }
}
