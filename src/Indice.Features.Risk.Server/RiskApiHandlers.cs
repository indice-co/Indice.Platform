using Indice.Features.Risk.Core;
using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Risk.Server;

internal static class RiskApiHandlers
{
    internal static async Task<Ok<OverallRuleExecutionResult>> GetRisk<TTransaction>(
        IRuleExecutionService<TTransaction> ruleExecutionService,
        ITransactionStore<TTransaction> transactionStore,
        TTransaction transaction
    ) where TTransaction : Transaction {
        // TODO: Handler acts as a service here that orchestrates calculation and persistence of transaction. Consider add a separate service in the future.
        // 1.Calculate the result based on the incoming transaction.
        var result = await ruleExecutionService.ExecuteAsync(transaction);
        // 2. Persist incoming transaction to the store.
        await transactionStore.CreateAsync(transaction);
        return TypedResults.Ok(result);
    }
}
