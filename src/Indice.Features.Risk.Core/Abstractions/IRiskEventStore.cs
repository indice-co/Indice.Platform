using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Store for risk engine events.</summary>
public interface IRiskEventStore
{
    /// <summary>Persists a new risk event in the store.</summary>
    /// <param name="event">The event occurred.</param>
    Task CreateAsync(RiskEvent @event);
    /// <summary></summary>
    /// <param name="subjectId"></param>
    /// <param name="type"></param>
    Task<IEnumerable<RiskEvent>> GetListByType(string subjectId, string type);
}
