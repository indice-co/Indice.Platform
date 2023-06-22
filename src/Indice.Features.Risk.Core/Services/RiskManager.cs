using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Configuration;
using Indice.Features.Risk.Core.Data.Models;

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
    /// <param name="transactionStore"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public RiskManager(
        IEnumerable<IRule<TTransaction>> rules,
        IEnumerable<RuleConfig> rulesConfiguration,
        IEventStore eventStore,
        ITransactionStore<TTransaction> transactionStore
    ) {
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        RulesConfiguration = rulesConfiguration ?? throw new ArgumentNullException(nameof(rulesConfiguration));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _transactionStore = transactionStore ?? throw new ArgumentNullException(nameof(transactionStore));
    }

    public IEnumerable<IRule<TTransaction>> Rules { get; }
    public IEnumerable<RuleConfig> RulesConfiguration { get; }

    public Task<int> CreateTransactionAsync(TTransaction transaction) => _transactionStore.CreateAsync(transaction);

    public Task<TTransaction?> GetTransactionByIdAsync(Guid transactionId) => _transactionStore.GetByIdAsync(transactionId);

    public async Task<AggregateRuleExecutionResult> GetTransactionRiskAsync(TTransaction transaction) {
        var results = new List<RuleExecutionResult>();
        foreach (var rule in Rules) {
            var result = await rule.ExecuteAsync(transaction);
            results.Add(result);
        }
        return new AggregateRuleExecutionResult(Rules.Count(), results);
    }
}
