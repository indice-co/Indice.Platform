using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Rules;

internal class GenericRule<TRiskEvent> : RiskRuleBase<TRiskEvent> where TRiskEvent : DbRiskEvent
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<IServiceProvider, TRiskEvent, ValueTask<RuleExecutionResult>> _ruleDelegate;

    public GenericRule(
        string name,
        IServiceProvider serviceProvider,
        Func<IServiceProvider, TRiskEvent, ValueTask<RuleExecutionResult>> ruleDelegate
    ) : base(name) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _ruleDelegate = ruleDelegate ?? throw new ArgumentNullException(nameof(ruleDelegate));
    }

    public override ValueTask<RuleExecutionResult> ExecuteAsync(TRiskEvent @event) =>
        _ruleDelegate.Invoke(_serviceProvider, @event);
}
