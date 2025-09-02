using Indice.Types;

namespace Indice.Features.Risk.Core.Models.Requests;

/// <summary>Options used to filter the list of risk events.</summary>
public class AdminRiskEventFilterRequest
{
    /// <summary>
    /// The list of filter clauses
    /// </summary>
    public FilterClause[] Filter { get; set; } = [];

    /// <summary>
    /// Two letter ISO codes for the countries.
    /// </summary>
    public FilterClause[] CountryIsoCode { get; set; } = [];

    /// <summary>
    /// The session ids for the events.
    /// </summary>
    public FilterClause[] SessionId { get; set; } = [];
}

