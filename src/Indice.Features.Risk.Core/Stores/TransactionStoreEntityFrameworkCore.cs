using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Risk.Core.Stores;

/// <summary>An implementation of <see cref="ITransactionStore{TTransaction}"/> that uses Entity Framework Core as a persistent storage mechanism.</summary>
/// <typeparam name="TTransaction">The type of the transaction.</typeparam>
public class TransactionStoreEntityFrameworkCore<TTransaction> : ITransactionStore<TTransaction> where TTransaction : TransactionBase
{
    private readonly RiskDbContext<TTransaction> _dbContext;

    /// <summary>Creates a new instance of <see cref="TransactionStoreEntityFrameworkCore{TTransaction}"/> class.</summary>
    /// <param name="dbContext">A <see cref="DbContext"/> for persisting transactions and it's related data.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public TransactionStoreEntityFrameworkCore(RiskDbContext<TTransaction> dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(IEnumerable<TTransaction> transactions) {
        _dbContext.AddRange(transactions);
        return await _dbContext.SaveChangesAsync();
    }
}
