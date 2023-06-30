using Indice.Features.Risk.Core;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Services;
using Indice.Features.Risk.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Risk.Server;

internal static class RiskApiHandlers
{
    internal static async Task<Ok<AggregateRuleExecutionResult>> GetRisk<TRiskEvent, TRiskRequest>(
        [FromServices] RiskManager<TRiskEvent> riskManager,
        [FromBody] TRiskRequest request
    ) where TRiskEvent : DbRiskEvent, new()
      where TRiskRequest : RiskRequestBase<TRiskEvent> {
        var riskEvent = request.ToDbRiskEvent();
        var result = await riskManager.GetRiskAsync(riskEvent);
        return TypedResults.Ok(result);
    }
}
