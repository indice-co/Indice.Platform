using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Core.Models;

/// <summary>The case type request model.</summary>
public class CaseTypeRequest
{
    /// <summary>The Id of the case type.</summary>
    public Guid? Id { get; set; }
    
    /// <summary>The Code of the case type.</summary>
    public string Code { get; set; } = null!;

    /// <summary>The Title of the case type.</summary>
    public string? Title { get; set; }

    /// <summary>The case type description.</summary>
    public string? Description { get; set; }

    /// <summary>The Data Schema of the case type</summary>
    public JsonNode? DataSchema { get; set; }

    /// <summary>the Layout of the case type</summary>
    public string? Layout { get; set; }

    /// <summary>The Translation for the case type</summary>
    public TranslationDictionary<CaseTypeTranslation>? Translations { get; set; }

    /// <summary>The Translation for the layout</summary>
    public Dictionary<string, string>? LayoutTranslations { get; set; }

    /// <summary>The case type tags.</summary>
    public string? Tags { get; set; }

    /// <summary>The case type configuration.</summary>
    public string? Config { get; set; }

    /// <summary>The allowed Roles that can create a new Case.</summary>
    public string? CanCreateRoles { get; set; }
    
    /// <summary>The order of the case type.</summary>
    public int? Order { get; set; }

    /// <summary>The flag for checking if the case type is a menu item or not.</summary>
    public bool IsMenuItem { get; set; }

    /// <summary>The filter configuration for the cases of the specified case type.</summary>
    public string? GridFilterConfig { get; set; }

    /// <summary>The column configuration for the cases of the specified case type.</summary>
    public string? GridColumnConfig { get; set; }
}
