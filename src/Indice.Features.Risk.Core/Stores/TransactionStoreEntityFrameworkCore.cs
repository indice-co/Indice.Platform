using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data;
using Indice.Features.Risk.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Risk.Core.Stores;

internal class TransactionStoreEntityFrameworkCore<TTransaction> : ITransactionStore<TTransaction> where TTransaction : Transaction
{
    private readonly RiskDbContext<TTransaction> _dbContext;

    public TransactionStoreEntityFrameworkCore(RiskDbContext<TTransaction> dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<int> CreateAsync(IEnumerable<TTransaction> transactions) {
        _dbContext.AddRange(transactions);
        return await _dbContext.SaveChangesAsync();
    }

    public Task<TTransaction?> GetByIdAsync(Guid transactionId) => 
        _dbContext.Transactions.Include(x => x.Events).FirstOrDefaultAsync(x => x.Id == transactionId);
}
