using Indice.Types;

namespace Indice.Features.Cases.Models.Responses;

/// <summary>The case type details model.</summary>
public class CaseType
{
    /// <summary>The Id of the case type.</summary>
    public Guid Id { get; set; }

    /// <summary>The case type code.</summary>
    public string Code { get; set; }

    /// <summary>The case type title.</summary>
    public string Title { get; set; }

    /// <summary>The case type description.</summary>
    public string Description { get; set; }
    
    /// <summary>The case type json schema.</summary>
    public string DataSchema { get; set; }

    /// <summary>The layout for the data schema.</summary>
    public string Layout { get; set; }

    /// <summary>The case type translations.</summary>
    public string Translations { get; set; }

    /// <summary>The layout translations.</summary>
    public string LayoutTranslations { get; set; }

    /// <summary>The case type tags.</summary>
    public string Tags { get; set; }

    /// <summary>The case type configuration.</summary>
    public string Config { get; set; }

    /// <summary>The allowed Roles that can create a new Case.</summary>
    public string CanCreateRoles { get; set; }

    /// <summary>The flag for checking if the case type is a menu item or not.</summary>
    public bool IsMenuItem { get; set; }

    /// <summary>The filter configuration for the cases of the specified case type.</summary>
    public string? GridFilterConfig { get; set; }

    /// <summary>The column configuration for the cases of the specified case type.</summary>
    public string? GridColumnConfig { get; set; }

    /// <summary>The checkpoints for this case type.</summary>
    public IEnumerable<CheckpointTypeDetails> CheckpointTypes { get; set; }

    /// <summary>Case type order.</summary>
    public int? Order { get; set; }

    #region Methods

    /// <summary>Translate helper</summary>
    /// <param name="culture"></param>
    /// <param name="includeTranslations"></param>
    /// <returns></returns>
    public CaseType Translate(string culture, bool includeTranslations) {
        var type = (CaseType)MemberwiseClone();
        if (!string.IsNullOrEmpty(culture) && Translations != null && TranslationDictionary<CaseTypeTranslation>.FromJson(Translations).TryGetValue(culture, out var translation)) {
            type.Title = translation.Title;
            type.Description = translation.Description;
        }
        if (!includeTranslations) {
            type.Translations = default;
        }
        return type;
    }

    #endregion
}
