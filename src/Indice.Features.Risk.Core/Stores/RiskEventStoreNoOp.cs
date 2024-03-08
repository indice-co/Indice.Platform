using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Risk.Core.Stores;

internal class RiskEventStoreNoOp : IRiskEventStore
{
    public Task CreateAsync(RiskEvent @event) => Task.CompletedTask;

    public Task<IEnumerable<RiskEvent>> GetList(string subjectId, string[]? types) => Task.FromResult(Enumerable.Empty<RiskEvent>());

    public Task<ResultSet<RiskEvent>> GetList(ListOptions<AdminRiskFilterRequest> options) => Task.FromResult(new ResultSet<RiskEvent>());

    public Task<IEnumerable<RiskEvent>> GetHistoricalRiskEvents(
        string subjectId, 
        DateTime startDate, 
        DateTime? endDate = null, 
        List<FilterClause>? filters = null, 
        string? type = null) => Task.FromResult(Enumerable.Empty<RiskEvent>());
}