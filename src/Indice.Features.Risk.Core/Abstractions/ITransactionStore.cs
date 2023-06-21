using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Store for risk engine transactions.</summary>
/// <typeparam name="TTransaction"></typeparam>
public interface ITransactionStore<TTransaction> where TTransaction : Transaction
{
    Task<int> CreateAsync(IEnumerable<TTransaction> transactions);
    Task<TTransaction?> GetByIdAsync(Guid transactionId);
}

public static class ITransactionStoreExtensions
{
    public static Task<int> CreateAsync<TTransaction>(this ITransactionStore<TTransaction> transactionStore, TTransaction transaction) where TTransaction : Transaction =>
        transactionStore.CreateAsync(new List<TTransaction> { transaction });
}
