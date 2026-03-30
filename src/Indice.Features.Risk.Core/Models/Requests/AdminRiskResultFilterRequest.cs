using Indice.Types;

namespace Indice.Features.Risk.Core.Models.Requests;

/// <summary>Options used to filter the list of risk results.</summary>
public class AdminRiskResultFilterRequest
{
    /// <summary>
    /// The list of filter clauses
    /// </summary>
    public FilterClause[] Filter { get; set; } = [];
}
