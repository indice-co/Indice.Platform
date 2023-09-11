using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary>Builder class used to configure the risk engine feature.</summary>
public class RiskRuleBuilder
{
    private readonly List<string> _ruleNames = new();
    private readonly IServiceCollection _services;

    internal RiskRuleBuilder(IServiceCollection services) {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>Adds a new rule to the risk engine by providing an implementation of <see cref="RiskRuleBase"/>.</summary>
    /// <typeparam name="TRule">The implementation type.</typeparam>
    /// <param name="name">The name of the rule.</param>
    /// <returns>The instance of <see cref="RiskRuleBuilder"/>.</returns>
    public RiskRuleBuilder AddRule<TRule>(string name) where TRule : RiskRuleBase {
        CheckAndAddRuleName(name);
        _services.AddTransient<RiskRuleBase, TRule>();
        return this;
    }

    /// <summary>Adds a new rule to the risk engine by providing a lambda expression.</summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="ruleDelegate">The delegate method to when a new event occurs.</param>
    /// <returns>The instance of <see cref="RiskRuleBuilder"/>.</returns>
    public RiskRuleBuilder AddRule(
        string name,
        Func<IServiceProvider, DbRiskEvent, ValueTask<RuleExecutionResult>> ruleDelegate
    ) {
        CheckAndAddRuleName(name);
        _services.AddTransient<RiskRuleBase>(serviceProvider => new GenericRule(name, serviceProvider, ruleDelegate));
        return this;
    }

    /// <summary>Adds a new rule to the risk engine by providing a lambda expression.</summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="ruleDelegate">The delegate method to when a new event occurs.</param>
    /// <returns>The instance of <see cref="RiskRuleBuilder"/>.</returns>
    public RiskRuleBuilder AddRule(
        string name,
        Func<DbRiskEvent, ValueTask<RuleExecutionResult>> ruleDelegate
    ) => AddRule(name, (serviceProvider, @event) => ruleDelegate(@event));

    /// <summary></summary>
    /// <param name="builder"></param>
    public void ConfigureEvents(Action<RiskEventConfigBuilder> builder) {
        var ruleConfigBuilder = new RiskEventConfigBuilder();
        builder?.Invoke(ruleConfigBuilder);
        var ruleConfigs = ruleConfigBuilder.Build();
        foreach (var ruleConfig in ruleConfigs.Events) {
            _services.AddSingleton(ruleConfig);
        }
    }

    private void CheckAndAddRuleName(string ruleName) {
        if (_ruleNames.Contains(ruleName, StringComparer.OrdinalIgnoreCase)) {
            throw new InvalidOperationException($"A rule with name {ruleName} is already configured in the risk engine.");
        }
        _ruleNames.Add(ruleName);
    }
}
