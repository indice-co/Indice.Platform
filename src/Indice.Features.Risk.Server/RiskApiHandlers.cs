using FluentValidation;
using Indice.Features.Risk.Core;
using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Services;
using Indice.Features.Risk.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Risk.Server;

internal static class RiskApiHandlers
{
    internal static async Task<Results<Ok<AggregateRuleExecutionResult>, NotFound>> GetTransactionRisk<TTransaction, TTransactionRequest>(
        [FromServices] RiskManager<TTransaction> riskManager,
        [FromBody] TTransactionRequest request
    ) where TTransaction : Transaction, new()
      where TTransactionRequest : CreateTransactionRequestBase<TTransaction> {
        TTransaction? transaction;
        if (!request.Id.HasValue) {
            transaction = request.ToTransaction();
            await riskManager.CreateTransactionAsync(transaction);
        } else {
            transaction = await riskManager.GetTransactionByIdAsync(request.Id.Value);
        }
        if (transaction is null) {
            return TypedResults.NotFound();
        }
        var result = await riskManager.GetTransactionRiskAsync(transaction);
        return TypedResults.Ok(result);
    }

    internal static async Task<Results<NoContent, ValidationProblem>> AddEvent(
        [FromServices] IEventStore eventStore,
        [FromServices] IValidator<CreateTransactionEventRequest> validator,
        [FromBody] CreateTransactionEventRequest request
    ) {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid) {
            return TypedResults.ValidationProblem(validationResult.ToDictionary(), detail: "Model state validation", extensions: new Dictionary<string, object?>() { ["code"] = "MODEL_STATE" });
        }
        await eventStore.CreateAsync(request.ToTransactionEvent());
        return TypedResults.NoContent();
    }
}
