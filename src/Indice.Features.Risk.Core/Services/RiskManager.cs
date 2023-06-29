using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Configuration;
using Indice.Features.Risk.Core.Data.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.Risk.Core.Services;

/// <summary>Manages transactions and events for the risk engine.</summary>
/// <typeparam name="TTransaction">The type of transaction.</typeparam>
public class RiskManager<TTransaction> where TTransaction : Transaction
{
    private readonly IEventStore _eventStore;
    private readonly ITransactionStore<TTransaction> _transactionStore;

    /// <summary>Creates a new instance of <see cref="RiskManager{TTransaction}"/>.</summary>
    /// <param name="rules">Collection of rules registered in the engine.</param>
    /// <param name="rulesConfiguration">Collection of rule configuration.</param>
    /// <param name="eventStore">Store for risk engine events.</param>
    /// <param name="transactionStore">Store for risk engine transactions.</param>
    /// <param name="riskEngineOptions">Options used to configure the core risk engine.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public RiskManager(
        IEnumerable<IRule<TTransaction>> rules,
        IEnumerable<RuleConfig> rulesConfiguration,
        IEventStore eventStore,
        ITransactionStore<TTransaction> transactionStore,
        IOptions<RiskEngineOptions> riskEngineOptions
    ) {
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        RulesConfiguration = rulesConfiguration ?? throw new ArgumentNullException(nameof(rulesConfiguration));
        RiskEngineOptions = riskEngineOptions.Value ?? throw new ArgumentNullException(nameof(riskEngineOptions));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _transactionStore = transactionStore ?? throw new ArgumentNullException(nameof(transactionStore));
    }

    /// <summary>The collection of rules registered in the risk engine.</summary>
    public IEnumerable<IRule<TTransaction>> Rules { get; }
    /// <summary>The collection of rule configuration provided in the risk engine.</summary>
    public IEnumerable<RuleConfig> RulesConfiguration { get; }
    /// <summary>Options used to configure the core risk engine.</summary>
    public RiskEngineOptions RiskEngineOptions { get; }

    /// <summary>Creates the given transaction in the underlying store.</summary>
    /// <param name="transaction">The transaction to persist.</param>
    public Task CreateTransactionAsync(TTransaction transaction) => _transactionStore.CreateAsync(transaction);

    /// <summary>Gets an existing transaction by it's unique id.</summary>
    /// <param name="transactionId">The transaction id to look for.</param>
    public Task<TTransaction?> GetTransactionByIdAsync(Guid transactionId) => _transactionStore.GetByIdAsync(transactionId);

    /// <summary>Gets the risk score for a given transaction.</summary>
    /// <param name="transaction">The transaction for which to calculate the risk score.</param>
    public async Task<AggregateRuleExecutionResult> GetTransactionRiskAsync(TTransaction transaction) {
        var results = new List<RuleExecutionResult>();
        foreach (var rule in Rules) {
            var result = await rule.ExecuteAsync(transaction);
            result.RuleName = rule.Name;
            var configuredEvents = RulesConfiguration
                .Where(ruleConfig => ruleConfig.RuleName == rule.Name)
                .SelectMany(ruleConfig => ruleConfig.Events);
            var finalRiskScore = result.RiskScore;
            foreach (var @event in transaction.Events) {
                var eventScore = configuredEvents.FirstOrDefault(x => x.EventName == @event.Name)?.Amount ?? 0;
                finalRiskScore += eventScore;
            }
            result.RiskScore = finalRiskScore;
            result.RiskLevel = RiskEngineOptions.RiskLevelRangeMapping.GetRiskLevel(result.RiskScore) ?? RiskLevel.VeryLow;
            results.Add(result);
        }
        return new AggregateRuleExecutionResult(transaction.Id, Rules.Count(), results);
    }
}
