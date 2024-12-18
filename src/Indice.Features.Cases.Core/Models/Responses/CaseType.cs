using System.Text.Json.Nodes;
using Indice.Types;

namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The case type details model.</summary>
public class CaseType
{
    /// <summary>The Id of the case type.</summary>
    public Guid Id { get; set; }

    /// <summary>The case type code.</summary>
    public string? Code { get; set; } = null!;

    /// <summary>The case type title.</summary>
    public string Title { get; set; } = null!;

    /// <summary>The case type description.</summary>
    public string? Description { get; set; }
    
    /// <summary>The case type json schema.</summary>
    public JsonNode? DataSchema { get; set; }

    /// <summary>The layout for the data schema.</summary>
    public JsonNode? Layout { get; set; }

    /// <summary>The case type translations.</summary>
    public TranslationDictionary<CaseTypeTranslation>? Translations { get; set; }

    /// <summary>The layout translations.</summary>
    public Dictionary<string, string>? LayoutTranslations { get; set; }

    /// <summary>The case type tags.</summary>
    public string? Tags { get; set; }

    /// <summary>The case type configuration.</summary>
    public JsonNode? Config { get; set; }

    /// <summary>The allowed Roles that can create a new Case.</summary>
    public string? CanCreateRoles { get; set; }

    /// <summary>The flag for checking if the case type is a menu item or not.</summary>
    public bool IsMenuItem { get; set; }

    /// <summary>The filter configuration for the cases of the specified case type.</summary>
    public JsonNode? GridFilterConfig { get; set; }

    /// <summary>The column configuration for the cases of the specified case type.</summary>
    public JsonNode? GridColumnConfig { get; set; }

    /// <summary>The checkpoints for this case type.</summary>
    public List<CheckpointTypeDetails> CheckpointTypes { get; set; } = [];

    /// <summary>Case type order.</summary>
    public int? Order { get; set; }
}
