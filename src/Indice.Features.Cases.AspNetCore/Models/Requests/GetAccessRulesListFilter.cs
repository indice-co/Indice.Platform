using Indice.Features.Cases.Data.Models;
using Indice.Types;

namespace Indice.Features.Cases.Models;

/// <summary>Options used to filter the list of MyCases.</summary>
public class GetAccessRulesListFilter
{
    /// <summary>Filter based on Role.</summary>
    public FilterClause? Role {get; set;}
    /// <summary>Filter based on Group.</summary>
    public FilterClause? GroupId { get; set; }
    /// <summary>The Checkpoint filter.</summary>
    public FilterClause? Checkpoint { get; set; }
    /// <summary>Case Type filter.</summary>
    public FilterClause? CaseType { get; set; }
}