using System.Text.Json;
using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Configuration;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Enums;
using Indice.Features.Risk.Core.Models.Requests;
using Indice.Types;
using Microsoft.Extensions.Options;

namespace Indice.Features.Risk.Core.Services;

/// <summary>Manages transactions and events for the risk engine.</summary>
public class RiskManager
{
    private readonly IRiskEventStore _riskEventStore;
    private readonly IRiskResultStore _riskResultStore;
    private readonly IRiskRuleStore _riskRuleStore;

    /// <summary>Creates a new instance of <see cref="RiskManager"/>.</summary>
    /// <param name="rules">Collection of rules registered in the engine.</param>
    /// <param name="riskEngineOptions">Options used to configure the core risk engine.</param>
    /// <param name="riskEventStore"></param>
    /// <param name="riskResultStore"></param>
    /// <param name="riskRuleStore"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public RiskManager(
        IEnumerable<RiskRule> rules,
        IOptions<RiskEngineOptions> riskEngineOptions,
        IRiskEventStore riskEventStore,
        IRiskResultStore riskResultStore,
        IRiskRuleStore riskRuleStore
    ) {
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _riskEventStore = riskEventStore ?? throw new ArgumentNullException(nameof(riskEventStore));
        _riskResultStore = riskResultStore ?? throw new ArgumentNullException(nameof(riskResultStore));
        _riskRuleStore = riskRuleStore ?? throw new ArgumentNullException(nameof(riskRuleStore));
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
        foreach (var rule in Rules.Where(x => x.Enabled)) {
            var result = await rule.ExecuteAsync(@event);
            result.RuleName = rule.Name;
            result.RiskLevel = RiskEngineOptions.RiskLevelRangeMapping.GetRiskLevel(result.RiskScore) ?? RiskLevel.None;
            results.Add(result);
        }
        return new AggregateRuleExecutionResult(@event.Id, Rules.Count(), results, RiskEngineOptions);
    }

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

    /// <summary>
    /// Gets the list of risk rules registered in the system.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<string>> GetRiskRulesAsync() {
        return await _riskRuleStore.GetList();
    }

    /// <summary>
    /// Gets the associated rule options, given a rule name.
    /// </summary>
    /// <param name="ruleName"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, string>> GetRiskRuleOptionsAsync(string ruleName) {
        return await _riskRuleStore.GetRiskRuleOptions(ruleName);
    }

    /// <summary>
    /// Updates the rule options, given a rule name.
    /// </summary>
    /// <param name="ruleName"></param>
    /// <param name="jsonData"></param>
    /// <returns></returns>
    public async Task UpdateRiskRuleOptionsAsync(string ruleName, JsonElement jsonData) {
        await _riskRuleStore.UpdateRiskRuleOptions(ruleName, jsonData);
    }

    /// <summary>Adds an event Id to risk result.</summary>
    /// <param name="resultId">The Id of the risk result.</param>
    /// <param name="eventId">The Id of the risk event.</param>
    internal async Task AddEventIdToRiskResultAsync(Guid resultId, Guid eventId) {
        await _riskResultStore.AddEventIdAsync(resultId, eventId);
    }
}
