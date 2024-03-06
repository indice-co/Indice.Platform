using Indice.Types;

namespace Indice.Features.Risk.Core.Models.Requests;

/// <summary>Options used to filter the list of risk events and risk results.</summary>
public class AdminRiskFilterRequest
{
    /// <summary>
    /// The list of filter clauses
    /// </summary>
    public FilterClause[] Filter { get; set; } = Array.Empty<FilterClause>();
}

