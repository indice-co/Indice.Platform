using Indice.Features.Risk.Core;
using Indice.Features.Risk.Core.Services;
using Indice.Features.Risk.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Risk.Server;

internal static class RiskApiHandlers
{
    internal static async Task<Ok<AggregateRuleExecutionResult>> GetRisk(
        [FromServices] RiskManager riskManager,
        [FromBody] RiskModel request
    ) {
        var riskEvent = request.ToRiskEvent();
        var result = await riskManager.GetRiskAsync(riskEvent);
        return TypedResults.Ok(result);
    }

    internal static async Task<StatusCodeHttpResult> CreateRiskEvent(
        [FromServices] RiskManager riskManager,
        [FromBody] RiskModel request
    ) {
        var riskEvent = request.ToRiskEvent();
        await riskManager.CreateRiskEventAsync(riskEvent);
        return TypedResults.StatusCode(StatusCodes.Status201Created);
    }
}
