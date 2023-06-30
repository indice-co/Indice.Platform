using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Abstracts a rule that is executed by the risk engine.</summary>
/// <typeparam name="TRiskEvent">The type of risk event.</typeparam>
public interface IRule<TRiskEvent> where TRiskEvent : DbRiskEvent
{
    /// <summary>The name of the rule.</summary>
    public string Name { get; }
    /// <summary>Executes the rule asynchronously.</summary>
    /// <param name="event">The event occurred.</param>
    /// <returns>The result of rule execution.</returns>
    ValueTask<RuleExecutionResult> ExecuteAsync(TRiskEvent @event);
}
