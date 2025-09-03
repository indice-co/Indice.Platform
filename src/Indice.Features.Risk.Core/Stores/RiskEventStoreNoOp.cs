using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Risk.Core.Stores;

internal class RiskEventStoreNoOp : IRiskEventStore
{
    public Task CreateAsync(DbRiskEvent @event) => Task.CompletedTask;

    public Task<IEnumerable<DbRiskEvent>> GetList(
        string subjectId, 
        string[]? types,
        DateTime? startDate,
        DateTime? endDate,
        List<FilterClause>? filters) => Task.FromResult(Enumerable.Empty<DbRiskEvent>());

    public Task<ResultSet<DbRiskEvent>> GetList(ListOptions<AdminRiskEventFilterRequest> options) => Task.FromResult(new ResultSet<DbRiskEvent>());

    public Task<IEnumerable<DbRiskEvent>> GetRiskEventsBySessionId(string sessionId) => Task.FromResult<IEnumerable<DbRiskEvent>>([]);
}