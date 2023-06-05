using Indice.Features.Risk.Core;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for configuring risk engine.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Adds the required services for configuring the risk engine, using the provided type <typeparamref name="TEvent"/>.</summary>
    /// <typeparam name="TEvent">The type of event that the engine manages.</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <returns>The <see cref="RiskEngineRuleBuilder{TEvent}"/> instance used to configure the risk engine.</returns>
    public static RiskEngineRuleBuilder<TEvent> AddRiskEngine<TEvent>(this IServiceCollection services) where TEvent : EventBase {
        var builder = new RiskEngineRuleBuilder<TEvent>(services);
        return builder;
    }

    /// <summary>Adds the required services for configuring the risk engine, using the default type <see cref="EventBase"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <returns>The <see cref="RiskEngineRuleBuilder{TEvent}"/> instance used to configure the risk engine.</returns>
    public static RiskEngineRuleBuilder<EventBase> AddRiskEngine(this IServiceCollection services) => 
        services.AddRiskEngine<EventBase>();
}
