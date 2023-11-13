using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data;
using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Stores;

internal class RiskResultStoreEntityFrameworkCore : IRiskResultStore
{
    private readonly RiskDbContext _dbContext;

    public RiskResultStoreEntityFrameworkCore(RiskDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task CreateAsync(DbAggregateRuleExecutionResult riskResult) {
        _dbContext.RiskResults.Add(riskResult);
        await _dbContext.SaveChangesAsync();
    }
}
