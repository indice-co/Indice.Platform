namespace Indice.Features.Risk.Core.Abstractions;

internal interface IRuleExecutionService<TTransaction> where TTransaction : TransactionBase
{
    Task<OverallRuleExecutionResult> ExecuteAsync(TTransaction transaction);
}
