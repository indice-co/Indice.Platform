using Indice.Features.Risk.Core.Abstractions;

namespace Indice.Features.Risk.Core;

internal class RuleExecutionService<TTransaction> : IRuleExecutionService<TTransaction> where TTransaction : TransactionBase
{
    private readonly IEnumerable<IRule<TTransaction>> _rules;

    public RuleExecutionService(IEnumerable<IRule<TTransaction>> rules) {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    public async Task<OverallRuleExecutionResult> ExecuteAsync(TTransaction transaction) {
        var results = new List<RuleExecutionResult>();
        foreach (var rule in _rules) {
            var result = await rule.ExecuteAsync(transaction);
            results.Add(result);
        }
        return new OverallRuleExecutionResult(_rules.Count(), results);
    }
}
