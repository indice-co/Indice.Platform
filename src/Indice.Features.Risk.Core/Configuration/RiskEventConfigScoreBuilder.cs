using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary></summary>
/// <typeparam name="TRiskEvent">The type of risk event.</typeparam>
public class RiskEventConfigScoreBuilder<TRiskEvent> where TRiskEvent : DbRiskEvent
{
    private readonly RiskEventConfigBuilder<TRiskEvent> _eventConfigBuilder;

    internal RiskEventConfigScoreBuilder(
        string eventName,
        RiskEventConfigBuilder<TRiskEvent> eventConfigBuilder
    ) {
        EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
        _eventConfigBuilder = eventConfigBuilder ?? throw new ArgumentNullException(nameof(eventConfigBuilder));
    }

    internal string EventName { get; }

    /// <summary></summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public RiskEventConfigBuilder<TRiskEvent> IncreaseBy(int amount) {
        var @event = _eventConfigBuilder.Events.FirstOrDefault(x => x.EventName == EventName);
        if (@event is not null) {
            @event.Amount += amount;
        }
        return _eventConfigBuilder;
    }

    /// <summary></summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public RiskEventConfigBuilder<TRiskEvent> ReduceBy(int amount) {
        var @event = _eventConfigBuilder.Events.FirstOrDefault(x => x.EventName == EventName);
        if (@event is not null) {
            @event.Amount -= amount;
        }
        return _eventConfigBuilder;
    }
}
