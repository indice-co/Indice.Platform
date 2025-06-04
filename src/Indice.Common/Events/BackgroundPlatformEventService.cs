using System.Threading.Channels;

namespace Indice.Events;

/// <summary>Implementation of <see cref="IPlatformEventService"/> that runs platform events asynchronously on the background using the <see cref="Channel"/> programming model.</summary>
internal sealed class BackgroundPlatformEventService : IPlatformEventService
{
    private readonly BackgroundPlatformEventServiceQueue _queue;

    public BackgroundPlatformEventService(BackgroundPlatformEventServiceQueue queue) {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    /// <inheritdoc />
    public async Task Publish(IPlatformEvent @event) => await _queue.EnqueueAsync(@event);
}