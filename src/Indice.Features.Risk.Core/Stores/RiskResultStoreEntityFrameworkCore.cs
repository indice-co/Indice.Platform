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

    public async Task UpdateRiskResultWithTransactionIdAsync(Guid? resultId, Guid? transactionId) {
        var riskResult = await _dbContext.RiskResults.FindAsync(resultId);
        riskResult.TransactionId = transactionId;
        await _dbContext.SaveChangesAsync(); 
    }
}
