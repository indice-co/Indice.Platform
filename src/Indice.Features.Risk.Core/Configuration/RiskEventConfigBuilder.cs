namespace Indice.Features.Risk.Core.Configuration;

/// <summary>Builder for configuring risk event score change.</summary>
public class RiskEventConfigBuilder
{
    internal List<RiskEventModel> Events { get; } = new List<RiskEventModel>();

    /// <summary></summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public RiskEventConfigScoreBuilder On(string eventName) {
        Events.Add(new RiskEventModel { 
            EventName = eventName
        });
        return new RiskEventConfigScoreBuilder(eventName, this);
    }

    internal RiskEventConfig Build() => new() {
        Events = Events
    };
}
