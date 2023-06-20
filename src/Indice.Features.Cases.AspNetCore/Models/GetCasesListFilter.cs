using Indice.Types;

namespace Indice.Features.Cases.Models;

/// <summary>
/// Options used to filter the list of cases.
/// 
/// Will be used internally to filter cases further, based on authentication parameters
/// </summary>
public class GetCasesListFilter
{
    /// <summary>
    /// A collection of properties to filter the cases by. It's list of FilterClause
    /// </summary>
    public List<FilterClause> CaseListData { get; set; } = new List<FilterClause>();

    /// <summary>The created date of the case, starting from, to filter.</summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>The create date of the case, ending to, to filter.</summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>The list of case type codes to filter.</summary>
    public List<string> CaseTypeCodes { get; set; }

    /// <summary>The list of checkpoint type Ids to filter.</summary>
    internal List<string> CheckpointTypeIds { get; set; }

    /// <summary>The list of checkpoint type codes to filter.</summary>
    public List<string> CheckpointTypeCodes { get; set; }

    /// <summary>The list of groupIds to filter.</summary>
    public List<string> GroupIds { get; set; }

    /// <summary>Construct filter clauses based on the metadata you are adding to the cases in your installation.</summary>
    public List<FilterClause> Metadata { get; set; }
}
