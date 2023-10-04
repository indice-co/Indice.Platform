using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Stores;

internal class RiskEventStoreNoOp : IRiskEventStore
{
    public Task CreateAsync(RiskEvent @event) => Task.CompletedTask;

    public Task<IEnumerable<RiskEvent>> GetList(string subjectId, string[]? types) => Task.FromResult(Enumerable.Empty<RiskEvent>());
}
