using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Services;

internal class RuleExecutionService<TTransaction> : IRuleExecutionService<TTransaction> where TTransaction : Transaction
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
