using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Store for risk engine events.</summary>
public interface IRiskEventStore
{
    /// <summary>Persists a new risk event in the store.</summary>
    /// <param name="event">The event occurred.</param>
    Task CreateAsync(RiskEvent @event);
    /// <summary></summary>
    /// <param name="subjectId">The subject id.</param>
    /// <param name="types">The event types.</param>
    Task<IEnumerable<RiskEvent>> GetList(string subjectId, string[]? types = null);
}
