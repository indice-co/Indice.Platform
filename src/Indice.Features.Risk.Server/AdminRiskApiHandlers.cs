using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models;
using Indice.Features.Risk.Core.Models.Requests;
using Indice.Features.Risk.Core.Models.Responses;
using Indice.Features.Risk.Core.Services;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Risk.Server;

internal static class AdminRiskApiHandlers
{
    internal static async Task<Ok<ResultSet<RiskEvent>>> GetRiskEvents(
        [FromServices] RiskStoreService riskStoreService,
        [AsParameters] ListOptions options,
        [AsParameters] AdminRiskEventFilterRequest filter
    ) {
        var events = await riskStoreService.GetRiskEventsAsync(ListOptions.Create(options, filter));
        var results = events.Items.Select(RiskEvent.FromDataModel).ToResultSet(events.Count);
        return TypedResults.Ok(results);
    }

    internal static async Task<Ok<ResultSet<DbAggregateRuleExecutionResult>>> GetRiskResults(
        [FromServices] RiskStoreService riskStoreService,
        [AsParameters] ListOptions options,
        [AsParameters] AdminRiskResultFilterRequest filter
    ) {
        var results = await riskStoreService.GetRiskResultsAsync(ListOptions.Create(options, filter));
        return TypedResults.Ok(results);
    }

    internal static async Task<Ok<IEnumerable<RiskEvent>>> GetRiskEventBySessionId(
        [FromServices] RiskStoreService riskStoreService,
        string sessionId
    ) {
        var riskEvents = await riskStoreService.GetRiskEventsBySessionIdAsync(sessionId);
        return TypedResults.Ok(riskEvents.Select(RiskEvent.FromDataModel));
    }

    internal static Ok<ResultSet<RiskRuleDto>> GetRiskRules(
        [FromServices] AdminRuleService adminRuleService
    ) {
        var results = adminRuleService.GetRiskRules();
        return TypedResults.Ok(results);
    }

    internal static async Task<Results<Ok<Dictionary<string, string?>>, NotFound>> GetRiskRuleOptions(
        [FromServices] AdminRuleService adminRuleService,
        string ruleName
    ) {
        var results = await adminRuleService.GetRiskRuleOptionsAsync(ruleName);
        return (results.Count() == 0) ? TypedResults.NotFound() : TypedResults.Ok(results);
    }

    internal static async Task<Results<NoContent, ValidationProblem>> UpdateRiskRuleOptions<TOptions>(
        [FromServices] AdminRuleService adminRuleService,
        [FromBody] TOptions request,
        string ruleName
    ) where TOptions : RuleOptions {
        await adminRuleService.UpdateRiskRuleOptionsAsync(ruleName, request);
        return TypedResults.NoContent();
    }
}
