using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data;
using Indice.Features.Risk.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Risk.Core.Stores;

internal class RiskEventStoreEntityFrameworkCore<TRiskEvent> : IRiskEventStore<TRiskEvent> where TRiskEvent : DbRiskEvent
{
    private readonly RiskDbContext<TRiskEvent> _dbContext;

    public RiskEventStoreEntityFrameworkCore(RiskDbContext<TRiskEvent> dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task CreateAsync(TRiskEvent @event) {
        _dbContext.RiskEvents.Add(@event);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<TRiskEvent>> GetListByType(string subjectId, string type) {
        var events = await _dbContext.RiskEvents.Where(x => x.SubjectId == subjectId && x.Type == type).ToListAsync();
        return events;
    }
}
