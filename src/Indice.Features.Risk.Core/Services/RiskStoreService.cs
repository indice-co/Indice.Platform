using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Risk.Core.Services;

/// <summary>Manages transactions and events for the risk engine.</summary>
public class RiskStoreService
{
    private readonly IRiskEventStore _riskEventStore;
    private readonly IRiskResultStore _riskResultStore;

    /// <summary>Creates a new instance of <see cref="RiskStoreService"/>.</summary>
    /// <param name="riskEventStore"></param>
    /// <param name="riskResultStore"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public RiskStoreService(
        IRiskEventStore riskEventStore,
        IRiskResultStore riskResultStore
    ) {
        _riskEventStore = riskEventStore ?? throw new ArgumentNullException(nameof(riskEventStore));
        _riskResultStore = riskResultStore ?? throw new ArgumentNullException(nameof(riskResultStore));
    }

    /// <summary>Creates a new event in the store.</summary>
    /// <param name="event">The event occurred and needs to be persisted.</param>
    public Task CreateRiskEventAsync(RiskEvent @event) =>
        _riskEventStore.CreateAsync(@event);

    /// <summary>Creates a new risk result in the store.</summary>
    /// <param name="riskResult">The calculated risk result needs to be persisted.</param>
    public Task CreateRiskResultAsync(DbAggregateRuleExecutionResult riskResult) =>
        _riskResultStore.CreateAsync(riskResult);

    /// <summary>Gets the list of events using the specified criteria.</summary>
    /// <param name="subjectId">The subject id.</param>
    /// <param name="types">The event types.</param>
    public Task<IEnumerable<RiskEvent>> GetRiskEventsAsync(string subjectId, string[]? types = null) =>
        _riskEventStore.GetList(subjectId, types);

    /// <summary>
    /// Gets the list of events using a given filter
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public async Task<ResultSet<RiskEvent>> GetRiskEventsAsync(ListOptions<AdminRiskFilterRequest> options) {
        return await _riskEventStore.GetList(options);
    }

    /// <summary>
    /// Gets the list of aggregate risk results using a given filter
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public async Task<ResultSet<DbAggregateRuleExecutionResult>> GetRiskResultsAsync(ListOptions<AdminRiskFilterRequest> options) {
        return await _riskResultStore.GetList(options);
    }

    /// <summary>Adds an event Id to risk result.</summary>
    /// <param name="resultId">The Id of the risk result.</param>
    /// <param name="eventId">The Id of the risk event.</param>
    internal async Task AddEventIdToRiskResultAsync(Guid resultId, Guid eventId) {
        await _riskResultStore.AddEventIdAsync(resultId, eventId);
    }
}
