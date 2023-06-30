using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Store for risk engine events.</summary>
/// <typeparam name="TRiskEvent">The type of risk event.</typeparam>
public interface IRiskEventStore<TRiskEvent> where TRiskEvent : DbRiskEvent
{
    /// <summary>Persists a new risk event in the store.</summary>
    /// <param name="event">The event occurred.</param>
    Task CreateAsync(TRiskEvent @event);
}
