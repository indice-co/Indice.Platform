namespace Indice.Features.Cases.Core.Models;

/// <summary>Options used to filter the list of MyCaseTypes.</summary>
public class GetMyCaseTypesListFilter
{
    /// <summary>The case type tag filter.</summary>
    public string[]? CaseTypeTags { get; set; }
}