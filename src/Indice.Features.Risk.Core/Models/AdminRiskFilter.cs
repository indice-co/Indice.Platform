using Indice.Types;

namespace Indice.Features.Risk.Core.Models;

/// <summary>Options used to filter the list of risk events and risk results.</summary>
public class AdminRiskFilter
{
    /// <summary>
    /// The list of filter clauses
    /// </summary>
    public FilterClause[] Filter { get; set; } = Array.Empty<FilterClause>();
}
