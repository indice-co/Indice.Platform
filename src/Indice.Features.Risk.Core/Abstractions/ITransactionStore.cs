namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>An abstraction for the underlying store used to persist the incoming transactions.</summary>
public class ITransactionStore<TTransaction> where TTransaction : TransactionBase
{
}
