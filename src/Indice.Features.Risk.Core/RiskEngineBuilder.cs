using Indice.Features.Risk.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Core;

/// <summary>Builder class used to configure the risk engine feature.</summary>
/// <typeparam name="TEvent">The type of event that the engine manages.</typeparam>
public class RiskEngineRuleBuilder<TEvent> where TEvent : EventBase
{
    internal RiskEngineRuleBuilder(IServiceCollection services) {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    internal IServiceCollection Services { get; }

    /// <summary>Adds a new rule to the risk engine by providing an implementation of <see cref="IRule{TEvent}"/>.</summary>
    /// <typeparam name="TRule">The implementation type.</typeparam>
    /// <returns>The instance of <see cref="RiskEngineRuleBuilder{TEvent}"/>.</returns>
    public RiskEngineRuleBuilder<TEvent> AddRule<TRule>() where TRule : class, IRule<TEvent> {
        Services.AddTransient<IRule<TEvent>, TRule>();
        return this;
    }

    /// <summary>Adds a new rule to the risk engine by providing a lambda expression.</summary>
    /// <param name="ruleDelegate">The delegate method to when a new event occurs.</param>
    /// <returns>The instance of <see cref="RiskEngineRuleBuilder{TEvent}"/>.</returns>
    public RiskEngineRuleBuilder<TEvent> AddRule(Func<IServiceProvider, TEvent, ValueTask<RiskLevel>> ruleDelegate) {
        Services.AddTransient<IRule<TEvent>>(serviceProvider => new DefaultRule<TEvent>(serviceProvider, ruleDelegate));
        return this;
    }

    /// <summary>Adds a new rule to the risk engine by providing a lambda expression.</summary>
    /// <param name="ruleDelegate">The delegate method to when a new event occurs.</param>
    /// <returns>The instance of <see cref="RiskEngineRuleBuilder{TEvent}"/>.</returns>
    public RiskEngineRuleBuilder<TEvent> AddRule(Func<TEvent, ValueTask<RiskLevel>> ruleDelegate) =>
        AddRule((serviceProvider, @event) => ruleDelegate(@event));
}
