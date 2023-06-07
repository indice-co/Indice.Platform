using Indice.Features.Risk.Core;
using Indice.Features.Risk.Core.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Risk.Server;

internal static class RiskApiHandlers
{
    internal static async Task<Ok<OverallRuleExecutionResult>> GetRisk<TTransaction>(
        IRuleExecutionService<TTransaction> ruleExecutionService,
        TTransaction transaction
    ) where TTransaction : TransactionBase {
        var result = await ruleExecutionService.ExecuteAsync(transaction);
        return TypedResults.Ok(result);
    }
}
