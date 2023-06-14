using FluentValidation;
using Indice.Features.Risk.Core;
using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Services;
using Indice.Features.Risk.Server.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Risk.Server;

internal static class RiskApiHandlers
{
    internal static async Task<Ok<OverallRuleExecutionResult>> GetTransactionRisk<TTransaction>(
        [FromServices] RiskManager<TTransaction> ruleExecutionService,
        [FromServices] ITransactionStore<TTransaction> transactionStore,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] TTransaction transaction
    ) where TTransaction : Transaction {
        var logger = loggerFactory.CreateLogger(nameof(RiskApiHandlers));
        // Calculate the result based on the incoming transaction.
        var result = await ruleExecutionService.ExecuteRulesAsync(transaction);
        // Persist incoming transaction to the store.
        var numberOfRowsAffected = await transactionStore.CreateAsync(transaction);
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
