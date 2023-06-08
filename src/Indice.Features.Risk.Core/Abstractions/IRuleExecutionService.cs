using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Abstractions;

internal interface IRuleExecutionService<TTransaction> where TTransaction : Transaction
{
    Task<OverallRuleExecutionResult> ExecuteAsync(TTransaction transaction);
}
