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

    /// <summary>
    /// Retrieves historical risk events for a specific SubjectId (User Id) starting from a given date.
    /// </summary>
    /// <param name="subjectId">The identifier of the subject (user) for which historical risk events are retrieved.</param>
    /// <param name="startDate">The start date from which historical risk events are considered.</param>
    /// <param name="endDate">Optional. The end date until which historical risk events are considered. If not provided, the current date is used.</param>
    /// <param name="filters">Optional. Additional filters to apply to the historical risk events.</param>
    /// <param name="type">Optional. The type to filter out specific historical risk events.</param>
    /// <returns>An enumerable collection of <see cref="RiskEvent"/> objects representing historical risk events.</returns>
    Task<IEnumerable<RiskEvent>> GetHistoricalRiskEvents(
        string subjectId,
        DateTime startDate,
        DateTime? endDate = null,
        List<FilterClause>? filters = null,
        string? type = null);
}