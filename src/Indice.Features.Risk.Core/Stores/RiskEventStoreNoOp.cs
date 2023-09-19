using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Stores;

internal class RiskEventStoreNoOp : IRiskEventStore
{
    public Task CreateAsync(RiskEvent @event) => Task.CompletedTask;

    public Task<IEnumerable<RiskEvent>> GetListByType(string subjectId, string type) => Task.FromResult(Enumerable.Empty<RiskEvent>());
}
