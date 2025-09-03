using Indice.Types;

namespace Indice.Features.Risk.Core.Models.Requests;

/// <summary>Options used to filter the list of risk events.</summary>
public class AdminRiskEventFilterRequest
{
    /// <summary>
    /// The list of filter clauses
    /// </summary>
    public FilterClause[] Filter { get; set; } = [];
}

