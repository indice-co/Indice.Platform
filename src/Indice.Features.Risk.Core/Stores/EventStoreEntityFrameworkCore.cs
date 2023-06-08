using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data;
using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Stores;

internal class EventStoreEntityFrameworkCore<TTransaction> : IEventStore where TTransaction : Transaction
{
    private readonly RiskDbContext<TTransaction> _dbContext;

    public EventStoreEntityFrameworkCore(RiskDbContext<TTransaction> dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<int> CreateAsync(TransactionEvent @event) {
        _dbContext.TransactionEvents.Add(@event);
        return await _dbContext.SaveChangesAsync();
    }
}
