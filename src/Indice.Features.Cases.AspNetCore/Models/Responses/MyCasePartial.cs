using System;
using Indice.Features.Cases.Data.Models;
using Indice.Types;

namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// The model for the customer with the minimum required properties.
    /// </summary>
    public class MyCasePartial
    {
        /// <summary>
        /// Id of the case.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The date the case was created.
        /// </summary>
        public DateTimeOffset? Created { get; set; }

        /// <summary>
        /// The current status of the case.
        /// </summary>
        public CasePublicStatus PublicStatus { get; set; }

        /// <summary>
        /// The case type code of the case.
        /// </summary>
        public string CaseTypeCode { get; set; }
        public string Title { get; set; }

        /// <summary>
        /// The checkpoint name of the case.
        /// </summary>
        public string Checkpoint { get; set; }

        /// <summary>
        /// The message that has been submitted from the backoffice.
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Translations.
        /// </summary>
        public TranslationDictionary<MyCasePartialTranslation>? Translations { get; set; }

        #region Methods

        /// <summary>
        /// Translate helper
        /// </summary>
        /// <param name="culture"></param>
        /// <param name="includeTranslations"></param>
        /// <returns></returns>
        public MyCasePartial Translate(string culture, bool includeTranslations) {
            var type = (MyCasePartial)MemberwiseClone();
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

    public class MyCasePartialTranslation
    {
        /// <summary>
        /// The title of the case type.
        /// </summary>
        public string Title { get; set; }
    }

}