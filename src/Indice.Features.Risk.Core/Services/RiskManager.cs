using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Configuration;
using Indice.Features.Risk.Core.Data.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.Risk.Core.Services;

/// <summary>Manages transactions and events for the risk engine.</summary>
public class RiskManager
{
    private readonly IRiskEventStore _riskEventStore;
    private readonly IRiskResultStore _riskResultStore;

    /// <summary>Creates a new instance of <see cref="RiskManager"/>.</summary>
    /// <param name="rules">Collection of rules registered in the engine.</param>
    /// <param name="riskEngineOptions">Options used to configure the core risk engine.</param>
    /// <param name="riskEventStore"></param>
    /// <param name="riskResultStore"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public RiskManager(
        IEnumerable<RiskRule> rules,
        IOptions<RiskEngineOptions> riskEngineOptions,
        IRiskEventStore riskEventStore,
        IRiskResultStore riskResultStore
    ) {
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _riskEventStore = riskEventStore ?? throw new ArgumentNullException(nameof(riskEventStore));
        _riskResultStore = riskResultStore ?? throw new ArgumentNullException(nameof(riskResultStore));
        RiskEngineOptions = riskEngineOptions.Value ?? throw new ArgumentNullException(nameof(riskEngineOptions));
    }

    /// <summary>The collection of rules registered in the risk engine.</summary>
    public IEnumerable<RiskRule> Rules { get; }
    /// <summary>Options used to configure the core risk engine.</summary>
    public RiskEngineOptions RiskEngineOptions { get; }

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

    /// <summary>Gets the risk score for a given event.</summary>
    /// <param name="event">The event occurred for which to calculate the risk score.</param>
    public async Task<AggregateRuleExecutionResult> GetRiskAsync(RiskEvent @event) {
        var results = new List<RuleExecutionResult>();
        foreach (var rule in Rules) {
            var result = await rule.ExecuteAsync(@event);
            result.RuleName = rule.Name;
            result.RiskLevel = RiskEngineOptions.RiskLevelRangeMapping.GetRiskLevel(result.RiskScore) ?? RiskLevel.None;
            results.Add(result);
        }
        return new AggregateRuleExecutionResult(@event.Id, Rules.Count(), results, RiskEngineOptions.RiskLevelRangeMapping);
    }

    /// <summary>Adds an event Id to risk result.</summary>
    /// <param name="resultId">The Id of the risk result.</param>
    /// <param name="eventId">The Id of the risk event.</param>
    internal async Task AddEventIdToRiskResultAsync(Guid resultId, Guid eventId) {
        await _riskResultStore.AddEventIdAsync(resultId, eventId);
    }
}
