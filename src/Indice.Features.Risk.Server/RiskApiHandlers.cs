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
    internal static async Task<Ok<OverallRuleExecutionResult>> GetTransactionRisk<TTransaction, TTransactionRequest>(
        [FromServices] RiskManager<TTransaction> ruleExecutionService,
        [FromServices] ITransactionStore<TTransaction> transactionStore,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] TTransactionRequest transactionRequest
    ) where TTransaction : Transaction
      where TTransactionRequest : CreateTransactionRequestBase {
        var logger = loggerFactory.CreateLogger(nameof(RiskApiHandlers));
        // Calculate the result based on the incoming transaction.
        var result = await ruleExecutionService.ExecuteRulesAsync(transactionRequest);
        // Persist incoming transaction to the store.
        var numberOfRowsAffected = await transactionStore.CreateAsync(transactionRequest);
        // Log success or failure.
        if (numberOfRowsAffected == 1) {
            logger.LogInformation("A transaction was created successfully.");
        } else {
            logger.LogError("Transaction could not be created.");
        }
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
