using Indice.Features.Cases.Data.Models;
using Indice.Types;

namespace Indice.Features.Cases.Models.Responses;

/// <summary>The model for the customer with the minimum required properties.</summary>
public class MyCasePartial
{
    /// <summary>Id of the case.</summary>
    public Guid Id { get; set; }

    /// <summary>The date the case was created.</summary>
    public DateTimeOffset? Created { get; set; }

    /// <summary>The case type code of the case.</summary>
    public string CaseTypeCode { get; set; }

    /// <summary>The case type title of the case.</summary>
    public string Title { get; set; }

    /// <summary>The checkpoint type of the case.</summary>    
    public CheckpointType CheckpointType { get; set; }

    /// <summary>The case metadata.</summary>
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>The message that has been submitted from the backoffice.</summary>
    public string Message { get; set; }

    /// <summary>Translations.</summary>
    public TranslationDictionary<MyCasePartialTranslation> Translations { get; set; }

    #region Methods

    /// <summary>Translate helper</summary>
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

        if (!string.IsNullOrEmpty(culture) && CheckpointType.Translations != null && CheckpointType.Translations.TryGetValue(culture, out var checkpointTypeTranslation)) {
            type.CheckpointType.Title = checkpointTypeTranslation.Title;
            type.CheckpointType.Description = checkpointTypeTranslation.Description;
        }

        return type;
    }

    #endregion
}

/// <summary>The translation wrapper for <see cref="MyCasePartial"/> model.</summary>
public class MyCasePartialTranslation
{
    /// <summary>The title of the case type.</summary>
    public string Title { get; set; }

    /// <summary>The checkpoint type.</summary>
    public CheckpointType CheckpointType { get; set; }
}