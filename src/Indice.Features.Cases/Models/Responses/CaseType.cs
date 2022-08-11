using System;
using Indice.Types;

namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// The case type model.
    /// </summary>
    public class CaseType
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
        /// The translations for the case type metadata (eg title).
        /// </summary>
        public TranslationDictionary<CaseTypeTranslation>? Translations { get; set; }

        #region Methods

        /// <summary>
        /// Translate helper
        /// </summary>
        /// <param name="culture"></param>
        /// <param name="includeTranslations"></param>
        /// <returns></returns>
        public CaseType Translate(string culture, bool includeTranslations) {
            var type = (CaseType)MemberwiseClone();
            if (!string.IsNullOrEmpty(culture) && Translations != null && Translations.TryGetValue(culture, out var translation)) {
                type.Title = translation.Title;
            }
            if (!includeTranslations) {
                type.Translations = default;
            }
            return type;
        }

        #endregion
    }
}