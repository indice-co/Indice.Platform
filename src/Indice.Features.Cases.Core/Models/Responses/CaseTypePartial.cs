using System.Text.Json.Nodes;
using Indice.Types;

namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The case type model.</summary>
public class CaseTypePartial
{
    /// <summary>The Id of the case type.</summary>
    public Guid Id { get; set; }

    /// <summary>The case type code.</summary>
    public string Code { get; set; } = null!;

    /// <summary>The case type title.</summary>
    public string? Title { get; set; }

    /// <summary>The case type description.</summary>
    public string? Description { get; set; }

    /// <summary>The case type json schema.</summary>
    public JsonNode? DataSchema { get; set; }

    /// <summary>The layout for the data schema.</summary>
    public JsonNode? Layout { get; set; }

    /// <summary>The layout translations for the data schema.</summary>
    public Dictionary<string, string>? LayoutTranslations { get; set; }

    /// <summary>The case type tags.</summary>
    public string? Tags { get; set; }

    /// <summary>The case type configuration.</summary>
    public JsonNode? Config { get; set; }

    /// <summary>The order which the case type will be shown.</summary>
    public int? Order { get; set; }

    /// <summary>The allowed Roles For case Creation.</summary>
    public List<string>? CanCreateRoles { get; set; }

    /// <summary>The case type category.</summary>
    public Category? Category { get; set; }

    /// <summary>The translations for the case type metadata (eg title).</summary>
    public TranslationDictionary<CaseTypeTranslation>? Translations { get; set; }

    /// <summary>The flag for checking if the case type is a menu item or not.</summary>
    public bool IsMenuItem { get; set; }

    /// <summary>The filter configuration for the cases of the specified case type.</summary>
    public string? GridFilterConfig { get; set; }

    /// <summary>The column configuration for the cases of the specified case type.</summary>
    public string? GridColumnConfig { get; set; }

    #region Methods

    /// <summary>Translate helper</summary>
    /// <param name="culture"></param>
    /// <param name="includeTranslations"></param>
    /// <returns></returns>
    public CaseTypePartial Translate(string culture, bool includeTranslations) {
        var type = (CaseTypePartial)MemberwiseClone();
        if (!string.IsNullOrEmpty(culture) && Translations != null && Translations.TryGetValue(culture, out var translation)) {
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