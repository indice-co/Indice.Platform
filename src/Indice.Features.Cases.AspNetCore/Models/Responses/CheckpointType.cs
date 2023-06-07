using Indice.Types;

namespace Indice.Features.Cases.Models.Responses;

/// <summary>The checkpoint model.</summary>
public class CheckpointType
{
    /// <summary>The Id of the <b>checkpoint type</b>.</summary>
    public Guid Id { get; set; }

    /// <summary>The code of the checkpoint.</summary>
    public string Code { get; set; }

    /// <summary>The title of the checkpoint.</summary>
    public string Title { get; set; }

    /// <summary>The description of the checkpoint.</summary>
    public string Description { get; set; }

    /// <summary>The translations of the checkpoint.</summary>
    public TranslationDictionary<CheckpointTypeTranslation> Translations { get; set; }

    #region Methods

    /// <summary>Translate helper</summary>
    /// <param name="culture"></param>
    /// <param name="includeTranslations"></param>
    /// <returns></returns>
    public CheckpointType Translate(string culture, bool includeTranslations) {
        var type = (CheckpointType)MemberwiseClone();
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
