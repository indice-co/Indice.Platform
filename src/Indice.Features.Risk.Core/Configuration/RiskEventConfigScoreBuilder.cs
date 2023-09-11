namespace Indice.Features.Risk.Core.Configuration;

/// <summary></summary>
public class RiskEventConfigScoreBuilder
{
    private readonly RiskEventConfigBuilder _eventConfigBuilder;

    internal RiskEventConfigScoreBuilder(
        string eventName,
        RiskEventConfigBuilder eventConfigBuilder
    ) {
        EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
        _eventConfigBuilder = eventConfigBuilder ?? throw new ArgumentNullException(nameof(eventConfigBuilder));
    }

    internal string EventName { get; }

    /// <summary></summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public RiskEventConfigBuilder IncreaseBy(int amount) {
        var @event = _eventConfigBuilder.Events.FirstOrDefault(x => x.EventName == EventName);
        if (@event is not null) {
            @event.Amount += amount;
        }
        return _eventConfigBuilder;
    }

    /// <summary></summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public RiskEventConfigBuilder ReduceBy(int amount) {
        var @event = _eventConfigBuilder.Events.FirstOrDefault(x => x.EventName == EventName);
        if (@event is not null) {
            @event.Amount -= amount;
        }
        return _eventConfigBuilder;
    }
}
