using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Store for risk engine events.</summary>
public interface IRiskEventStore
{
    /// <summary>Persists a new risk event in the store.</summary>
    /// <param name="event">The event occurred.</param>
    Task CreateAsync(RiskEvent @event);

    /// <summary>Returns the risk events associated with a given subject ID</summary>
    /// <param name="subjectId">The subject id.</param>
    /// <param name="types">The event types.</param>
    Task<IEnumerable<RiskEvent>> GetList(string subjectId, string[]? types = null);

    /// <summary>
    /// Fetches risk events from the store
    /// </summary>
    /// <param name="options"></param>
    Task<ResultSet<RiskEvent>> GetList(ListOptions<AdminRiskFilterRequest> options);
}
