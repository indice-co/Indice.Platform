using Indice.Types;

namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>Case Type Category model.</summary>
public class Category
{
    /// <summary>The Id of the category</summary>
    public Guid Id { get; set; }
    /// <summary>The Code of the category</summary>
    public string Name { get; set; } = null!;
    /// <summary>the Description of the category</summary>
    public string? Description { get; set; }
    /// <summary>The Order of the category</summary>
    public int? Order { get; set; }
    /// <summary>The translations for the category properties</summary>
    public TranslationDictionary<CategoryTranslation>? Translations { get; set; }

    #region Methods
    /// <summary>Translation method</summary>
    /// <param name="culture">either Greek or English</param>
    /// <param name="includeTranslations">boolean for if we want to return the translation with the rest of the model.</param>
    /// <returns></returns>
    public Category Translate(string culture, bool includeTranslations) {
        var type = (Category)MemberwiseClone();
        if (!string.IsNullOrEmpty(culture) && Translations != null && Translations.TryGetValue(culture, out var translation)) {
            type.Name = translation.Name!;
            type.Description = translation.Description;
        }
        if (!includeTranslations) {
            type.Translations = default;
        }
        return type;
    }

    #endregion
}
