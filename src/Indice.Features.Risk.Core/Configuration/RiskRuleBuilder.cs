using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary>Builder class used to configure the risk engine feature.</summary>
/// <typeparam name="TRiskEvent">The type of risk event.</typeparam>
public class RiskRuleBuilder<TRiskEvent> where TRiskEvent : DbRiskEvent
{
    private readonly List<string> _ruleNames = new();
    private readonly IServiceCollection _services;

    internal RiskRuleBuilder(IServiceCollection services) {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>Adds a new rule to the risk engine by providing an implementation of <see cref="RiskRuleBase{TRiskEvent}"/>.</summary>
    /// <typeparam name="TRule">The implementation type.</typeparam>
    /// <param name="name">The name of the rule.</param>
    /// <returns>The instance of <see cref="RiskRuleBuilder{TRiskEvent}"/>.</returns>
    public RiskRuleBuilder<TRiskEvent> AddRule<TRule>(string name) where TRule : RiskRuleBase<TRiskEvent> {
        CheckAndAddRuleName(name);
        _services.AddTransient<RiskRuleBase<TRiskEvent>, TRule>();
        return this;
    }

    /// <summary>Adds a new rule to the risk engine by providing a lambda expression.</summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="ruleDelegate">The delegate method to when a new event occurs.</param>
    /// <returns>The instance of <see cref="RiskRuleBuilder{TRiskEvent}"/>.</returns>
    public RiskRuleBuilder<TRiskEvent> AddRule(
        string name,
        Func<IServiceProvider, TRiskEvent, ValueTask<RuleExecutionResult>> ruleDelegate
    ) {
        CheckAndAddRuleName(name);
        _services.AddTransient<RiskRuleBase<TRiskEvent>>(serviceProvider => new GenericRule<TRiskEvent>(name, serviceProvider, ruleDelegate));
        return this;
    }

    /// <summary>Adds a new rule to the risk engine by providing a lambda expression.</summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="ruleDelegate">The delegate method to when a new event occurs.</param>
    /// <returns>The instance of <see cref="RiskRuleBuilder{TRiskEvent}"/>.</returns>
    public RiskRuleBuilder<TRiskEvent> AddRule(
        string name,
        Func<TRiskEvent, ValueTask<RuleExecutionResult>> ruleDelegate
    ) => AddRule(name, (serviceProvider, @event) => ruleDelegate(@event));

    /// <summary></summary>
    /// <param name="builder"></param>
    public void ConfigureEvents(Action<RiskEventConfigBuilder<TRiskEvent>> builder) {
        var ruleConfigBuilder = new RiskEventConfigBuilder<TRiskEvent>();
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
