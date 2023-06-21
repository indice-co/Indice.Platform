using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary></summary>
public class RuleConfigBuilder<TTransaction> where TTransaction : Transaction
{
    internal RuleConfigBuilder(string ruleName) {
        RuleName = ruleName;
    }

    internal string RuleName { get; }
    internal List<RuleEvent> Events { get; } = new List<RuleEvent>();

    /// <summary></summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public RuleEventActionConfigBuilder<TTransaction> On(string eventName) {
        Events.Add(new RuleEvent { 
            EventName = eventName
        });
        return new RuleEventActionConfigBuilder<TTransaction>(eventName, this);
    }

    internal RuleConfig Build() => new() {
        RuleName = RuleName,
        Events = Events
    };
}
