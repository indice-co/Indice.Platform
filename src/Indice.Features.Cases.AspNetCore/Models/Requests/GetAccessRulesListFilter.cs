using Indice.Features.Cases.Data.Models;
using Indice.Types;

namespace Indice.Features.Cases.Models;

/// <summary>Options used to filter the list of MyCases.</summary>
public class GetAccessRulesListFilter
{
    /// <summary>The Checkpoints filter.</summary>
    public List<string> Checkpoints { get; set; }
    /// <summary>Construct filter clauses based on case data.</summary>
    public List<FilterClause> Data { get; set; }
    /// <summary>Construct filter clauses based on case metadata.</summary>
    public List<FilterClause> Metadata { get; set; }
}