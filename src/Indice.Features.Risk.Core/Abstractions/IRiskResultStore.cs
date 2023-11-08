using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Store for risk engine results.</summary>
public interface IRiskResultStore
{
    /// <summary>Persists a new risk result in the store.</summary>
    /// <param name="riskResult">The calculated risk result.</param>
    Task CreateAsync(DbAggregateRuleExecutionResult riskResult);
}
