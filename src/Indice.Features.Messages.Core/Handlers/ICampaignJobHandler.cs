namespace Indice.Features.Messages.Core.Handlers;

/// <summary>Abstracts the various implementations of campaign job handlers.</summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
public interface ICampaignJobHandler<in TEvent> where TEvent : class
{
    /// <summary>Contains processing logic for the campaign job.</summary>
    /// <param name="event">The message data.</param>
    Task Process(TEvent @event);
}
