using Indice.Features.GeoIP;
using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models;
using Indice.Features.Risk.Core.Models.Requests;
using Indice.Features.Risk.Core.Models.Responses;
using Indice.Types;

namespace Indice.Features.Risk.Core.Services;

/// <summary>Manages transactions and events for the risk engine.</summary>
public class RiskStoreService
{
    private readonly IRiskEventStore _riskEventStore;
    private readonly IRiskResultStore _riskResultStore;
    private readonly IPAddressLocator _ipAddressLocator;

    /// <summary>Creates a new instance of <see cref="RiskStoreService"/>.</summary>
    /// <param name="riskEventStore"></param>
    /// <param name="riskResultStore"></param>
    /// <param name="ipAddressLocator"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public RiskStoreService(
        IRiskEventStore riskEventStore,
        IRiskResultStore riskResultStore,
        IPAddressLocator ipAddressLocator) {
        _riskEventStore = riskEventStore ?? throw new ArgumentNullException(nameof(riskEventStore));
        _riskResultStore = riskResultStore ?? throw new ArgumentNullException(nameof(riskResultStore));
        _ipAddressLocator = ipAddressLocator ?? throw new ArgumentNullException(nameof(ipAddressLocator));
    }

    /// <summary>Creates a new event in the store.</summary>
    /// <param name="model">The event occurred and needs to be persisted.</param>
    public async Task<RiskEvent> CreateRiskEventAsync(RiskModel model) {
        var dbEvent = RiskMapper.ToRiskEvent(model, _ipAddressLocator);
        return RiskMapper.EventFromDbModel(await _riskEventStore.CreateAsync(dbEvent));
    }

    /// <summary>Creates a new risk result in the store.</summary>
    /// <param name="riskResult">The calculated risk result needs to be persisted.</param>
    public Task CreateRiskResultAsync(DbAggregateRuleExecutionResult riskResult) =>
        _riskResultStore.CreateAsync(riskResult);

    /// <summary>Gets the list of events using the specified criteria.</summary>
    /// <param name="subjectId">The subject id.</param>
    /// <param name="names">The event names.</param>
    public async Task<IEnumerable<RiskEvent>> GetRiskEventsAsync(string subjectId, string[]? names = null) =>
        (await _riskEventStore.GetList(subjectId, names)).Select(RiskMapper.EventFromDbModel);

    /// <summary>
    /// Gets the list of events using a given filter
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public async Task<ResultSet<RiskEvent>> GetRiskEventsAsync(ListOptions<AdminRiskEventFilterRequest> options) {
        var dbEvents = await _riskEventStore.GetList(options);
        return dbEvents.Items.Select(RiskMapper.EventFromDbModel).ToResultSet(dbEvents.Count);
    }

    /// <summary>
    /// Gets the list of aggregate risk results using a given filter
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public async Task<ResultSet<DbAggregateRuleExecutionResult>> GetRiskResultsAsync(ListOptions<AdminRiskResultFilterRequest> options) {
        return await _riskResultStore.GetList(options);
    }

    /// <summary>
    /// Fetches risk events by session id
    /// </summary>
    /// <param name="sessionId">The session id associated with the risk events</param>
    /// <returns>A collection of risk events</returns>
    public async Task<IEnumerable<RiskEvent>> GetRiskEventsBySessionIdAsync(string sessionId) {
        return (await _riskEventStore.GetRiskEventsBySessionId(sessionId)).Select(RiskMapper.EventFromDbModel);
    }

    /// <summary>Adds an event Id to risk result.</summary>
    /// <param name="resultId">The Id of the risk result.</param>
    /// <param name="eventId">The Id of the risk event.</param>
    internal async Task AddEventIdToRiskResultAsync(Guid resultId, Guid eventId) {
        await _riskResultStore.AddEventIdAsync(resultId, eventId);
    }
}
