using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Store for risk engine events.</summary>
public interface IEventStore
{
    /// <summary></summary>
    /// <param name="event"></param>
    Task CreateAsync(TransactionEvent @event);
}
