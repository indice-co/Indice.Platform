using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Store for risk engine transactions.</summary>
/// <typeparam name="TTransaction">The type of transaction.</typeparam>
public interface ITransactionStore<TTransaction> where TTransaction : Transaction
{
    /// <summary>Creates the given transactions in the underlying store.</summary>
    /// <param name="transactions">The collection of transactions to persist.</param>
    /// <returns></returns>
    Task CreateAsync(IEnumerable<TTransaction> transactions);
    /// <summary>Get's an existing transaction by it's unique id.</summary>
    /// <param name="transactionId">The transaction id to look for.</param>
    Task<TTransaction?> GetByIdAsync(Guid transactionId);
}

/// <summary>Extension methods on <see cref="ITransactionStore{TTransaction}"/> interface.</summary>
public static class ITransactionStoreExtensions
{
    /// <summary>Creates the given transaction in the underlying store.</summary>
    /// <typeparam name="TTransaction">The type of transaction.</typeparam>
    /// <param name="transactionStore">Store for risk engine transactions.</param>
    /// <param name="transaction">The transaction to persist.</param>
    public static Task CreateAsync<TTransaction>(this ITransactionStore<TTransaction> transactionStore, TTransaction transaction) where TTransaction : Transaction =>
        transactionStore.CreateAsync(new List<TTransaction> { transaction });
}
