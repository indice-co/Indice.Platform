using Indice.Features.Risk.Core;
using Indice.Features.Risk.Core.Models;
using Indice.Features.Risk.Core.Services;
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
        var riskResult = result.ToRiskResult(request);
        await riskManager.CreateRiskResultAsync(riskResult);
        return TypedResults.Ok(result);
    }

    internal static async Task<StatusCodeHttpResult> CreateRiskEvent(
        [FromServices] RiskManager riskManager,
        [FromBody] RiskModel request
    ) {
        var riskEvent = request.ToRiskEvent();
        await riskManager.CreateRiskEventAsync(riskEvent);
        if (request.ResultId != null) {
            await riskManager.AddEventIdToRiskResultAsync((Guid)request.ResultId, riskEvent.Id);
        }
        return TypedResults.StatusCode(StatusCodes.Status201Created);
    }
}
