using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Rules;

internal class GenericRule<TRiskEvent> : IRule<TRiskEvent> where TRiskEvent : DbRiskEvent
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<IServiceProvider, TRiskEvent, ValueTask<RuleExecutionResult>> _ruleDelegate;

    public GenericRule(
        IServiceProvider serviceProvider,
        Func<IServiceProvider, TRiskEvent, ValueTask<RuleExecutionResult>> ruleDelegate
    ) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _ruleDelegate = ruleDelegate ?? throw new ArgumentNullException(nameof(ruleDelegate));
    }

    public string Name { get; internal set; } = null!;

    public ValueTask<RuleExecutionResult> ExecuteAsync(TRiskEvent transaction) =>
        _ruleDelegate.Invoke(_serviceProvider, transaction);
}
