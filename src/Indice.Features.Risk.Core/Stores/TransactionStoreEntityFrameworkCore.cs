using Indice.Features.Risk.Core.Abstractions;

namespace Indice.Features.Risk.Core.Stores;

/// <summary>An implementation of <see cref="ITransactionStore{TTransaction}"/> that uses Entity Framework Core as a persistent storage mechanism.</summary>
/// <typeparam name="TTransaction"></typeparam>
public class TransactionStoreEntityFrameworkCore<TTransaction> : ITransactionStore<TTransaction> where TTransaction : TransactionBase
{
}
