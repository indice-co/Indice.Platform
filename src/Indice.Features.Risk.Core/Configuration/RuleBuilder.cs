using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary>Builder class used to configure the risk engine feature.</summary>
/// <typeparam name="TRiskEvent">The type of risk event.</typeparam>
public class RuleBuilder<TRiskEvent> where TRiskEvent : DbRiskEvent
{
    private readonly List<string> _ruleNames = new();
    private readonly IServiceCollection _services;

    internal RuleBuilder(IServiceCollection services) {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>Adds a new rule to the risk engine by providing an implementation of <see cref="IRule{TRiskEvent}"/>.</summary>
    /// <typeparam name="TRule">The implementation type.</typeparam>
    /// <param name="name">The name of the rule.</param>
    /// <param name="builder">Action for configuring the change of score calculation based on incoming events.</param>
    /// <returns>The instance of <see cref="RuleBuilder{TRiskEvent}"/>.</returns>
    public RuleBuilder<TRiskEvent> AddRule<TRule>(string name, Action<EventConfigBuilder<TRiskEvent>>? builder = null) where TRule : class, IRule<TRiskEvent> {
        CheckAndAddRuleName(name);
        ConfigureRules(name, builder);
        _services.AddTransient<IRule<TRiskEvent>, TRule>();
        return this;
    }

    /// <summary>Adds a new rule to the risk engine by providing a lambda expression.</summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="ruleDelegate">The delegate method to when a new event occurs.</param>
    /// <returns>The instance of <see cref="RuleBuilder{TRiskEvent}"/>.</returns>
    public RuleBuilder<TRiskEvent> AddRule(
        string name,
        Func<IServiceProvider, TRiskEvent, ValueTask<RuleExecutionResult>> ruleDelegate
    ) {
        CheckAndAddRuleName(name);
        ConfigureRules(name);
        _services.AddTransient<IRule<TRiskEvent>>(serviceProvider => new GenericRule<TRiskEvent>(serviceProvider, ruleDelegate) {
            Name = name
        });
        return this;
    }

    /// <summary>Adds a new rule to the risk engine by providing a lambda expression.</summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="ruleDelegate">The delegate method to when a new event occurs.</param>
    /// <returns>The instance of <see cref="RuleBuilder{TRiskEvent}"/>.</returns>
    public RuleBuilder<TRiskEvent> AddRule(
        string name,
        Func<TRiskEvent, ValueTask<RuleExecutionResult>> ruleDelegate
    ) => AddRule(name, (serviceProvider, @event) => ruleDelegate(@event));

    /// <summary></summary>
    /// <param name="eventsConfigAction"></param>
    public void ConfigureEvents(Action<EventConfigBuilder<TRiskEvent>> eventsConfigAction) { }

    private void ConfigureRules(string name, Action<EventConfigBuilder<TRiskEvent>>? builder = null) {
        var ruleConfigBuilder = new EventConfigBuilder<TRiskEvent>(name);
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
