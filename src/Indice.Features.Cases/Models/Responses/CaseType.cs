using System;
using Indice.Types;

namespace Indice.Features.Cases.Models.Responses
{
    public class CaseType
    {
        public Guid Id { get; set; }
        public string? Code { get; set; }
        public string? Title { get; set; }
        public string? DataSchema { get; set; }
        public string? Layout { get; set; }
        /// <summary>
        /// Translations.
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