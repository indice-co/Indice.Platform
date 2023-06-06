using Indice.Features.Risk.Core.Abstractions;

namespace Indice.Features.Risk.Core;

internal class GenericRule<TTransaction> : IRule<TTransaction> where TTransaction : TransactionBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<IServiceProvider, TTransaction, ValueTask<RuleExecutionResult>> _ruleDelegate;

    public GenericRule(
        IServiceProvider serviceProvider,
        string ruleName,
        Func<IServiceProvider, TTransaction, ValueTask<RuleExecutionResult>> ruleDelegate
    ) {
        Name = ruleName;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _ruleDelegate = ruleDelegate ?? throw new ArgumentNullException(nameof(ruleDelegate));
    }

    public string Name { get; }

    public ValueTask<RuleExecutionResult> ExecuteAsync(TTransaction transaction) =>
        _ruleDelegate.Invoke(_serviceProvider, transaction);
}
