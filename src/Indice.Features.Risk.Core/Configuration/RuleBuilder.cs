using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary>Builder class used to configure the risk engine feature.</summary>
/// <typeparam name="TTransaction">The type of transaction that the engine manages.</typeparam>
public class RuleBuilder<TTransaction> where TTransaction : Transaction
{
    private readonly List<string> _ruleNames = new();
    private readonly IServiceCollection _services;

    internal RuleBuilder(IServiceCollection services) {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>Adds a new rule to the risk engine by providing an implementation of <see cref="IRule{TTransaction}"/>.</summary>
    /// <typeparam name="TRule">The implementation type.</typeparam>
    /// <param name="name">The name of the rule.</param>
    /// <param name="builder">Action for configuring the change of score calculation based incoming events.</param>
    /// <returns>The instance of <see cref="RuleBuilder{TTransaction}"/>.</returns>
    public RuleBuilder<TTransaction> AddRule<TRule>(string name, Action<RuleConfigBuilder<TTransaction>>? builder = null) where TRule : class, IRule<TTransaction> {
        CheckAndAddRuleName(name);
        ConfigureRules(name, builder);
        _services.AddTransient<IRule<TTransaction>, TRule>();
        return this;
    }

    /// <summary>Adds a new rule to the risk engine by providing a lambda expression.</summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="ruleDelegate">The delegate method to when a new transaction occurs.</param>
    /// <param name="builder">Action for configuring the change of score calculation based incoming events.</param>
    /// <returns>The instance of <see cref="RuleBuilder{TTransaction}"/>.</returns>
    public RuleBuilder<TTransaction> AddRule(
        string name,
        Func<IServiceProvider, TTransaction, ValueTask<RuleExecutionResult>> ruleDelegate,
        Action<RuleConfigBuilder<TTransaction>>? builder = null
    ) {
        CheckAndAddRuleName(name);
        ConfigureRules(name, builder);
        _services.AddTransient<IRule<TTransaction>>(serviceProvider => {
            var genericRule = new GenericRule<TTransaction>(serviceProvider, ruleDelegate);
            genericRule.Name = name;
            return genericRule;
        });
        return this;
    }

    /// <summary>Adds a new rule to the risk engine by providing a lambda expression.</summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="ruleDelegate">The delegate method to when a new transaction occurs.</param>
    /// <param name="builder">Action for configuring the change of score calculation based incoming events.</param>
    /// <returns>The instance of <see cref="RuleBuilder{TTransaction}"/>.</returns>
    public RuleBuilder<TTransaction> AddRule(
        string name,
        Func<TTransaction, ValueTask<RuleExecutionResult>> ruleDelegate,
        Action<RuleConfigBuilder<TTransaction>>? builder = null
    ) => AddRule(name, (serviceProvider, transaction) => ruleDelegate(transaction), builder);

    private void ConfigureRules(string name, Action<RuleConfigBuilder<TTransaction>>? builder = null) {
        var ruleConfigBuilder = new RuleConfigBuilder<TTransaction>(name);
        builder?.Invoke(ruleConfigBuilder);
        var ruleConfig = ruleConfigBuilder.Build();
        _services.AddSingleton(ruleConfig);
    }

    private void CheckAndAddRuleName(string ruleName) {
        if (_ruleNames.Contains(ruleName, StringComparer.OrdinalIgnoreCase)) {
            throw new InvalidOperationException($"A rule with name {ruleName} is already configured in the risk engine.");
        }
        _ruleNames.Add(ruleName);
    }
}
