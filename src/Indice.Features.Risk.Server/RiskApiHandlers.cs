using Indice.Features.GeoIP;
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
        [FromServices] RiskStoreService riskStoreService,
        [FromServices] RiskService riskService,
        [FromBody] RiskModel request
    ) {
        var result = await riskService.GetRiskAsync(request);
        var riskResult = result.ToDbAggregateExecutionRiskResult(request);
        await riskStoreService.CreateRiskResultAsync(riskResult);
        return TypedResults.Ok(result);
    }

    internal static async Task<StatusCodeHttpResult> CreateRiskEvent(
        [FromServices] RiskStoreService riskStoreService,
        [FromBody] RiskModel request
    ) {
        var riskEvent = await riskStoreService.CreateRiskEventAsync(request);
        if (request.ResultId != null || request.ResultId == Guid.Empty) {
            await riskStoreService.AddEventIdToRiskResultAsync((Guid)request.ResultId, riskEvent.Id);
        }
        return TypedResults.StatusCode(StatusCodes.Status201Created);
    }
}
