using Indice.Features.Risk.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Core;

/// <summary>Builder class used to configure the risk engine feature.</summary>
/// <typeparam name="TTransaction">The type of transaction that the engine manages.</typeparam>
public class RiskEngineRuleBuilder<TTransaction> where TTransaction : TransactionBase
{
    internal RiskEngineRuleBuilder(IServiceCollection services) {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    internal IServiceCollection Services { get; }

    /// <summary>Adds a new rule to the risk engine by providing an implementation of <see cref="IRule{TTransaction}"/>.</summary>
    /// <typeparam name="TRule">The implementation type.</typeparam>
    /// <returns>The instance of <see cref="RiskEngineRuleBuilder{TTransaction}"/>.</returns>
    public RiskEngineRuleBuilder<TTransaction> AddRule<TRule>() where TRule : class, IRule<TTransaction> {
        Services.AddTransient<IRule<TTransaction>, TRule>();
        return this;
    }

    /// <summary>Adds a new rule to the risk engine by providing a lambda expression.</summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="ruleDelegate">The delegate method to when a new transaction occurs.</param>
    /// <returns>The instance of <see cref="RiskEngineRuleBuilder{TTransaction}"/>.</returns>
    public RiskEngineRuleBuilder<TTransaction> AddRule(string name, Func<IServiceProvider, TTransaction, ValueTask<RuleExecutionResult>> ruleDelegate) {
        Services.AddTransient<IRule<TTransaction>>(serviceProvider => new GenericRule<TTransaction>(serviceProvider, name, ruleDelegate));
        return this;
    }

    /// <summary>Adds a new rule to the risk engine by providing a lambda expression.</summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="ruleDelegate">The delegate method to when a new transaction occurs.</param>
    /// <returns>The instance of <see cref="RiskEngineRuleBuilder{TTransaction}"/>.</returns>
    public RiskEngineRuleBuilder<TTransaction> AddRule(string name, Func<TTransaction, ValueTask<RuleExecutionResult>> ruleDelegate) =>
        AddRule(name, (serviceProvider, transaction) => ruleDelegate(transaction));
}
