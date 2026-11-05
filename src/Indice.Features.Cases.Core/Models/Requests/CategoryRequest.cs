using Indice.Features.Cases.Core.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Core.Models.Requests;

/// <summary>The category create/update request model.</summary>
public class CategoryRequest
{
    /// <summary>The name of the category.</summary>
    public string Name { get; set; } = null!;

    /// <summary>The description of the category.</summary>
    public string? Description { get; set; }

    /// <summary>The order of the category.</summary>
    public int? Order { get; set; }

    /// <summary>The translations of the category.</summary>
    public TranslationDictionary<CategoryTranslation>? Translations { get; set; }
}
