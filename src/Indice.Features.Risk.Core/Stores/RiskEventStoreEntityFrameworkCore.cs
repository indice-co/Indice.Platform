using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data;
using Indice.Features.Risk.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Risk.Core.Stores;

internal class RiskEventStoreEntityFrameworkCore : IRiskEventStore
{
    private readonly RiskDbContext _dbContext;

    public RiskEventStoreEntityFrameworkCore(RiskDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task CreateAsync(RiskEvent @event) {
        _dbContext.RiskEvents.Add(@event);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<RiskEvent>> GetList(string subjectId, string[]? types) {
        var query = _dbContext
            .RiskEvents
            .Where(x => x.SubjectId == subjectId);
        if (types?.Any() == true) {
            query = query.Where(x => types.Contains(x.Type));
        }
        return await query.ToListAsync();
    }
}
