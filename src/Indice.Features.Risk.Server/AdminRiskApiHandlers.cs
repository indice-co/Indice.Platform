using System.Text.Json;
using FluentValidation;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models.Requests;
using Indice.Features.Risk.Core.Services;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Server;

internal static class AdminRiskApiHandlers
{
    internal static async Task<Ok<ResultSet<RiskEvent>>> GetRiskEvents(
        [FromServices] RiskManager riskManager,
        [AsParameters] ListOptions options,
        [AsParameters] AdminRiskFilterRequest filter
    ) {
        var results = await riskManager.GetRiskEventsAsync(ListOptions.Create(options, filter));
        return TypedResults.Ok(results);
    }

    internal static async Task<Ok<ResultSet<DbAggregateRuleExecutionResult>>> GetRiskResults(
        [FromServices] RiskManager riskManager,
        [AsParameters] ListOptions options,
        [AsParameters] AdminRiskFilterRequest filter
    ) {
        var results = await riskManager.GetRiskResultsAsync(ListOptions.Create(options, filter));
        return TypedResults.Ok(results);
    }

    internal static async Task<Ok<IEnumerable<string>>> GetRiskRules(
        [FromServices] RiskManager riskManager
    ) {
        var results = await riskManager.GetRiskRulesAsync();
        return TypedResults.Ok(results);
    }

    internal static async Task<Results<Ok<Dictionary<string, string>>, NotFound>> GetRiskRuleOptions(
        [FromServices] RiskManager riskManager,
        string ruleName
    ) {
        var results = await riskManager.GetRiskRuleOptionsAsync(ruleName);
        return (results.Count() == 0) ? TypedResults.NotFound() : TypedResults.Ok(results);
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem, BadRequest>> UpdateRiskRuleOptions(
        [FromServices] RiskManager riskManager,
        [FromServices] IServiceProvider serviceProvider,
        [FromBody] JsonElement request,
        string ruleName
    ) {
        var validator = serviceProvider.GetRequiredKeyedService<IValidator>(ruleName);
        if (validator is null) {
            return TypedResults.BadRequest();
        }

        var result = validator?.ValidateDynamic(request);
        if (!result.IsValid) {
            return TypedResults.ValidationProblem(result.ToDictionary());
        }

        await riskManager.UpdateRiskRuleOptionsAsync(ruleName, request);
        return TypedResults.NoContent();
    }
}
