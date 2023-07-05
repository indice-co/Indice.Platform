using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary>Builder for configuring risk event score change.</summary>
/// <typeparam name="TRiskEvent">The type of risk event.</typeparam>
public class RiskEventConfigBuilder<TRiskEvent> where TRiskEvent : DbRiskEvent
{
    internal List<RiskEventModel> Events { get; } = new List<RiskEventModel>();

    /// <summary></summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public RiskEventConfigScoreBuilder<TRiskEvent> On(string eventName) {
        Events.Add(new RiskEventModel { 
            EventName = eventName
        });
        return new RiskEventConfigScoreBuilder<TRiskEvent>(eventName, this);
    }

    internal RiskEventConfig Build() => new() {
        Events = Events
    };
}
