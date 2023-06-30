using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary>Builder for configuring risk event score change.</summary>
/// <typeparam name="TRiskEvent">The type of risk event.</typeparam>
public class EventConfigBuilder<TRiskEvent> where TRiskEvent : DbRiskEvent
{
    internal EventConfigBuilder(string ruleName) {
        RuleName = ruleName;
    }

    internal string RuleName { get; }
    internal List<RiskEventModel> Events { get; } = new List<RiskEventModel>();

    /// <summary></summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public EventConfigScoreBuilder<TRiskEvent> On(string eventName) {
        Events.Add(new RiskEventModel { 
            EventName = eventName
        });
        return new EventConfigScoreBuilder<TRiskEvent>(eventName, this);
    }

    internal RuleConfig Build() => new() {
        RuleName = RuleName,
        Events = Events
    };
}
