using System.Text.Json;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models;
using Indice.Features.Risk.Core.Models.Requests;
using Indice.Features.Risk.Core.Services;
using Indice.Serialization;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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

    internal static Ok<List<string>> GetRiskRules(
        [FromServices] AdminRiskManager adminRiskManager
    ) {
        var results = adminRiskManager.GetRiskRules();
        return TypedResults.Ok(results);
    }

    internal static async Task<Results<Ok<Dictionary<string, string>>, NotFound>> GetRiskRuleOptions(
        [FromServices] AdminRiskManager adminRiskManager,
        string ruleName
    ) {
        var results = await adminRiskManager.GetRiskRuleOptionsAsync(ruleName);
        return (results.Count() == 0) ? TypedResults.NotFound() : TypedResults.Ok(results);
    }

    internal static async Task<Results<NoContent, ValidationProblem>> UpdateRiskRuleOptions<TOptions>(
        [FromServices] AdminRiskManager adminRiskManager,
        [FromBody] TOptions request,
        string ruleName
    ) where TOptions : RuleOptionsBase {
        var options = JsonSerializerOptionDefaults.GetDefaultSettings();
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(request, options));
        await adminRiskManager.UpdateRiskRuleOptionsAsync(ruleName, jsonElement);
        return TypedResults.NoContent();
    }
}
