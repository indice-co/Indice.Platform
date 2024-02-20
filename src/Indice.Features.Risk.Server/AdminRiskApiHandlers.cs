using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models;
using Indice.Features.Risk.Core.Services;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Risk.Server;

internal static class AdminRiskApiHandlers {
    internal static async Task<Ok<ResultSet<RiskEvent>>> GetRiskEvents(
        [FromServices] RiskManager riskManager,
        [AsParameters] ListOptions options,
        [AsParameters] AdminRiskFilter filter
    ) {
        var results = await riskManager.GetRiskEventsAsync(ListOptions.Create(options, filter));
        return TypedResults.Ok(results);
    }

    internal static async Task<Ok<ResultSet<DbAggregateRuleExecutionResult>>> GetRiskResults(
        [FromServices] RiskManager riskManager,
        [AsParameters] ListOptions options,
        [AsParameters] AdminRiskFilter filter
    ) {
        var results = await riskManager.GetRiskResultsAsync(ListOptions.Create(options, filter));
        return TypedResults.Ok(results);
    }
}
