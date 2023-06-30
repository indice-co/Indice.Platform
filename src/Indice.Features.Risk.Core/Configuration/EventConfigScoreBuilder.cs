using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary></summary>
/// <typeparam name="TRiskEvent">The type of risk event.</typeparam>
public class EventConfigScoreBuilder<TRiskEvent> where TRiskEvent : DbRiskEvent
{
    private readonly EventConfigBuilder<TRiskEvent> _eventConfigBuilder;

    internal EventConfigScoreBuilder(
        string eventName,
        EventConfigBuilder<TRiskEvent> eventConfigBuilder
    ) {
        EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
        _eventConfigBuilder = eventConfigBuilder ?? throw new ArgumentNullException(nameof(eventConfigBuilder));
    }

    internal string EventName { get; }

    /// <summary></summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public EventConfigBuilder<TRiskEvent> IncreaseBy(int amount) {
        var @event = _eventConfigBuilder.Events.FirstOrDefault(x => x.EventName == EventName);
        if (@event is not null) {
            @event.Amount += amount;
        }
        return _eventConfigBuilder;
    }

    /// <summary></summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public EventConfigBuilder<TRiskEvent> ReduceBy(int amount) {
        var @event = _eventConfigBuilder.Events.FirstOrDefault(x => x.EventName == EventName);
        if (@event is not null) {
            @event.Amount -= amount;
        }
        return _eventConfigBuilder;
    }
}
