#if !NETSTANDARD2_1
using System.Threading.Channels;

namespace Indice.Events;

/// <summary>Implementation of <see cref="IPlatformEventService"/> that runs platform events asynchronously on the background using the <see cref="Channel"/> programming model.</summary>
public class BackgroundPlatformEventService : IPlatformEventService
{
    /// <inheritdoc />
    public Task Publish<TEvent>(TEvent @event) where TEvent : IPlatformEvent {
        throw new NotImplementedException();
    }
}
#endif