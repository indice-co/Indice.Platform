using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Configuration;
using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Services;

internal class RiskManager<TTransaction> where TTransaction : Transaction
{
    private readonly IEventStore _eventStore;

    public RiskManager(
        IEnumerable<IRule<TTransaction>> rules,
        IEnumerable<RuleConfig> ruleConfigurations,
        IEventStore eventStore
    ) {
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        RuleConfigurations = ruleConfigurations ?? throw new ArgumentNullException(nameof(ruleConfigurations));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
    }

    public IEnumerable<IRule<TTransaction>> Rules { get; }
    public IEnumerable<RuleConfig> RuleConfigurations { get; }

    public async Task<OverallRuleExecutionResult> ExecuteRulesAsync(TTransaction transaction) {
        var results = new List<RuleExecutionResult>();
        foreach (var rule in Rules) {
            var result = await rule.ExecuteAsync(transaction);
            results.Add(result);
        }
        return new OverallRuleExecutionResult(Rules.Count(), results);
    }
}
