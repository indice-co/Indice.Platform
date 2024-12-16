using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Core.Data.Models;

#pragma warning disable 1591
public class DbCaseType
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public string Code { get; set; } = null!;
    public string? Title { get; set; }
    public string? Description { get; set; }        
    public JsonNode? DataSchema { get; set; }
    public JsonNode? Layout { get; set; }
    public TranslationDictionary<CaseTypeTranslation>? Translations { get; set; }
    public Dictionary<string, string>? LayoutTranslations { get; set; }
    public string? Tags { get; set; }
    public JsonNode? Config { get; set; }
    public int? Order { get; set; }
    /// <summary>The allowed Roles that can create a new Case</summary>
    public string? CanCreateRoles { get; set; }
    public bool IsMenuItem { get; set; } 
    public string? GridFilterConfig { get; set; }
    public string? GridColumnConfig { get; set; }
    /// <summary>Available checkpoints for this case type</summary>
    public virtual List<DbCheckpointType> CheckpointTypes { get; set; } = [];
    public virtual DbCategory? Category { get; set; }
}
#pragma warning restore 1591
