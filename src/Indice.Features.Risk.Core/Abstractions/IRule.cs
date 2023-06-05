namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Abstracts a rule that is executed by the risk engine.</summary>
/// <typeparam name="TEvent">The type of event that the engine manages.</typeparam>
public interface IRule<TEvent> where TEvent : EventBase
{
    /// <summary>Executes the rule asynchronously.</summary>
    /// <param name="event">The event occurred.</param>
    /// <returns>The calculated risk level.</returns>
    ValueTask<RiskLevel> ExecuteAsync(TEvent @event);
}
