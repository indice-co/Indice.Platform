using Indice.Types;

namespace Indice.Features.Cases.Core.Models;

/// <summary>Options used to filter the list of MyCases.</summary>
public class GetMyCasesListFilter
{
    /// <summary>The case type tag filter.</summary>
    public string[]? CaseTypeTags { get; set; }
    /// <summary>The case status filter.</summary>
    public CaseStatus[]? Statuses { get; set; }
    /// <summary>The case type code filter.</summary>
    public string[]? CaseTypeCodes { get; set; }
    /// <summary>The CreatedFrom filter.</summary>
    public DateTimeOffset? CreatedFrom { get; set; }
    /// <summary>The CreatedTo filter.</summary>
    public DateTimeOffset? CreatedTo { get; set; }
    /// <summary>The CompletedFrom filter.</summary>
    public DateTimeOffset? CompletedFrom { get; set; }
    /// <summary>The CompletedTo filter.</summary>
    public DateTimeOffset? CompletedTo { get; set; }
    /// <summary>The Checkpoints filter.</summary>
    public string[]? Checkpoints { get; set; }
    /// <summary>Construct filter clauses based on case data.</summary>
    public FilterClause[]? Data { get; set; }
    /// <summary>Construct filter clauses based on case metadata.</summary>
    public FilterClause[]? Metadata { get; set; }
    /// <summary>Determines whether draft cases should be included in result</summary>
    public bool? IncludeDrafts { get; set; }
    /// <summary>The reference number filter.</summary>
    public int[]? ReferenceNumbers { get; set; }
    /// <summary>Determines whether case data should be included in result.</summary>
    public bool? IncludeData { get; set; }
}
