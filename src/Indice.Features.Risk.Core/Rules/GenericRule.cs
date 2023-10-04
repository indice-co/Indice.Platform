using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Rules;

internal class GenericRule : RiskRule
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<IServiceProvider, RiskEvent, ValueTask<RuleExecutionResult>> _ruleDelegate;

    public GenericRule(
        string name,
        IServiceProvider serviceProvider,
        Func<IServiceProvider, RiskEvent, ValueTask<RuleExecutionResult>> ruleDelegate
    ) : base(name) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _ruleDelegate = ruleDelegate ?? throw new ArgumentNullException(nameof(ruleDelegate));
    }

    public override ValueTask<RuleExecutionResult> ExecuteAsync(RiskEvent @event) =>
        _ruleDelegate.Invoke(_serviceProvider, @event);
}
