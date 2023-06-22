using FluentValidation;
using Indice.Features.Risk.Core;
using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Services;
using Indice.Features.Risk.Server.Commands;
using Indice.Features.Risk.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Risk.Server;

internal static class RiskApiHandlers
{
    internal static async Task<Ok<AggregateRuleExecutionResult>> GetTransactionRisk<TTransaction, TTransactionRequest>(
        [FromServices] RiskManager<TTransaction> riskManager,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] TTransactionRequest transactionRequest
    ) where TTransaction : Transaction, new()
      where TTransactionRequest : CreateTransactionRequestBase<TTransaction> {
        var logger = loggerFactory.CreateLogger(nameof(RiskApiHandlers));
        TTransaction? transaction = null;
        if (!transactionRequest.Id.HasValue) {
            transaction = transactionRequest.ToTransaction();
            await riskManager.CreateTransactionAsync(transaction);
        } else {
            transaction = await riskManager.GetTransactionByIdAsync(transactionRequest.Id.Value);
        }
        var result = await riskManager.GetTransactionRiskAsync(transaction!);
        return TypedResults.Ok(result);
    }

    internal static async Task<Results<NoContent, ValidationProblem>> AddEvent(
        [FromServices] IEventStore eventStore,
        [FromServices] ILoggerFactory loggerFactory,
        [FromServices] IValidator<CreateTransactionEventCommand> validator,
        [FromBody] CreateTransactionEventCommand command
    ) {
        var validationResult = await validator.ValidateAsync(command);
        if (!validationResult.IsValid) {
            return TypedResults.ValidationProblem(validationResult.ToDictionary(), detail: "Model state validation", extensions: new Dictionary<string, object?>() { ["code"] = "MODEL_STATE" });
        }
        var logger = loggerFactory.CreateLogger(nameof(RiskApiHandlers));
        // Persist incoming event to the store.
        var numberOfRowsAffected = await eventStore.CreateAsync(command.ToTransactionEvent());
        if (numberOfRowsAffected == 1) {
            logger.LogInformation("A transaction was created successfully.");
        } else {
            logger.LogError("Transaction could not be created.");
        }
        return TypedResults.NoContent();
    }
}
