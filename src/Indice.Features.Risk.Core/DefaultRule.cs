using Indice.Features.Risk.Core.Abstractions;

namespace Indice.Features.Risk.Core;

internal class DefaultRule<TEvent> : IRule<TEvent> where TEvent : EventBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<IServiceProvider, TEvent, ValueTask<RiskLevel>> _ruleDelegate;

    public DefaultRule(
        IServiceProvider serviceProvider,
        Func<IServiceProvider, TEvent, ValueTask<RiskLevel>> ruleDelegate
    ) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _ruleDelegate = ruleDelegate ?? throw new ArgumentNullException(nameof(ruleDelegate));
    }

    public ValueTask<RiskLevel> ExecuteAsync(TEvent @event) => 
        _ruleDelegate.Invoke(_serviceProvider, @event);
}
