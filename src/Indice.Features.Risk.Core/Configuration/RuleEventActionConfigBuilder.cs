using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary></summary>
public class RuleEventActionConfigBuilder<TTransaction> where TTransaction : Transaction
{
    private readonly RuleConfigBuilder<TTransaction> _riskEngineRuleEventBuilder;

    internal RuleEventActionConfigBuilder(
        string eventName,
        RuleConfigBuilder<TTransaction> riskEngineRuleEventBuilder
    ) {
        EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
        _riskEngineRuleEventBuilder = riskEngineRuleEventBuilder ?? throw new ArgumentNullException(nameof(riskEngineRuleEventBuilder));
    }

    internal string EventName { get; }

    /// <summary></summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public RuleConfigBuilder<TTransaction> IncreaseBy(int amount) {
        var @event = _riskEngineRuleEventBuilder.Events.FirstOrDefault(x => x.EventName == EventName);
        if (@event is not null) {
            @event.Amount += amount;
        }
        return _riskEngineRuleEventBuilder;
    }

    /// <summary></summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public RuleConfigBuilder<TTransaction> ReduceBy(int amount) {
        var @event = _riskEngineRuleEventBuilder.Events.FirstOrDefault(x => x.EventName == EventName);
        if (@event is not null) {
            @event.Amount -= amount;
        }
        return _riskEngineRuleEventBuilder;
    }
}
