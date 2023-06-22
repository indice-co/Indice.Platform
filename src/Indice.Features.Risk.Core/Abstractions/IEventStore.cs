using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Store for risk engine events.</summary>
public interface IEventStore
{
    Task<int> CreateAsync(TransactionEvent @event);
}
