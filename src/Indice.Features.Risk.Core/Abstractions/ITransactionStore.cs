namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>An abstraction for the underlying store used to persist the incoming transactions.</summary>
/// <typeparam name="TTransaction">The type of the transaction.</typeparam>
public interface ITransactionStore<TTransaction> where TTransaction : TransactionBase
{
    /// <summary>Inserts the given transactions in the database.</summary>
    /// <param name="transactions">The transactions to persist.</param>
    /// <returns>The number of rows inserted.</returns>
    Task<int> CreateAsync(IEnumerable<TTransaction> transactions);
}

/// <summary></summary>
public static class ITransactionStoreExtensions
{
    /// <summary>Inserts the given transaction in the database.</summary>
    /// <typeparam name="TTransaction">The type of the transaction.</typeparam>
    /// <param name="transactionStore">An abstraction for the underlying store used to persist the incoming transactions.</param>
    /// <param name="transaction">The transactions to persist.</param>
    /// <returns>The number of rows inserted.</returns>
    public static Task<int> CreateAsync<TTransaction>(this ITransactionStore<TTransaction> transactionStore, TTransaction transaction) where TTransaction : TransactionBase =>
        transactionStore.CreateAsync(new List<TTransaction> { transaction });
}
