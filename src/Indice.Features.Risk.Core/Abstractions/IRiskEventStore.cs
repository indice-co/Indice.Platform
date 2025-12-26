using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Store for risk engine events.</summary>
public interface IRiskEventStore
{
    /// <summary>Persists a new risk event in the store.</summary>
    /// <param name="event">The event occurred.</param>
    Task<DbRiskEvent> CreateAsync(DbRiskEvent @event);

    /// <summary>Returns the risk events associated with a given subject ID</summary>
    /// <param name="subjectId">The identifier of the subject (user) for which risk events are retrieved.</param>
    /// <param name="names">Optional. The event names to filter out specific risk events.</param>
    /// <param name="startDate">Optional. The start date from which risk events are considered.</param>
    /// <param name="endDate">Optional. The end date until which risk events are considered. If not provided, the current date is used.</param>
    /// <param name="filters">Optional. Additional filters to apply to the risk events.</param>
    Task<IEnumerable<DbRiskEvent>> GetList(
        string subjectId,
        string[]? names = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        List<FilterClause>? filters = null);

    /// <summary>
    /// Fetches risk events from the store
    /// </summary>
    /// <param name="options"></param>
    Task<ResultSet<DbRiskEvent>> GetList(ListOptions<AdminRiskEventFilterRequest> options);

    /// <summary>
    /// Fetches risk events by session id
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    Task<IEnumerable<DbRiskEvent>> GetRiskEventsBySessionId(string sessionId);
}
