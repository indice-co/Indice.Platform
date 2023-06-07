using Indice.Features.Risk.Core.Abstractions;

namespace Indice.Features.Risk.Core;

internal class RuleExecutionService<TTransaction> : IRuleExecutionService<TTransaction> where TTransaction : TransactionBase
{
    private readonly IEnumerable<IRule<TTransaction>> _rules;
    private readonly ITransactionStore<TTransaction> _transactionStore;

    public RuleExecutionService(
        IEnumerable<IRule<TTransaction>> rules,
        ITransactionStore<TTransaction> transactionStore
    ) {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _transactionStore = transactionStore ?? throw new ArgumentNullException(nameof(transactionStore));
    }

    public async Task<OverallRuleExecutionResult> ExecuteAsync(TTransaction transaction) {
        var results = new List<RuleExecutionResult>();
        foreach (var rule in _rules) {
            var result = await rule.ExecuteAsync(transaction);
            result.RuleName = rule.Name;
            results.Add(result);
        }
        await _transactionStore.CreateAsync(transaction);
        return new OverallRuleExecutionResult(_rules.Count(), results);
    }
}
