using System.Text.Json.Serialization;
using Indice.Types;

namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The checkpoint type model.</summary>
public class CheckpointType
{
    /// <summary>The Id of the <b>checkpoint type</b>.</summary>
    public Guid Id { get; set; }

    /// <summary>The code of the <b>checkpoint type</b>.</summary>
    public string Code { get; set; } = null!;

    /// <summary>The title of the <b>checkpoint type</b>.</summary>
    public string? Title { get; set; }

    /// <summary>The description of the <b>checkpoint type</b>.</summary>
    public string? Description { get; set; }

    /// <summary>The status of the case. This is the external status for the customer.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))] // Because of Elsa.HttpActivities the default serializer is newtonsoft
    public CaseStatus? Status { get; set; }

    /// <summary>Indicates if the checkpoint type is private, which means not visible to the customer.</summary>
    public bool? Private { get; set; }

    /// <summary>The translations of the <b>checkpoint type</b>.</summary>
    public TranslationDictionary<CheckpointTypeTranslation>? Translations { get; set; }

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
