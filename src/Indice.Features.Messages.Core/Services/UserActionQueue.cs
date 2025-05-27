using System.Threading.Channels;
using Indice.Features.Messages.Core.Events;

namespace Indice.Features.Messages.Core.Services;
internal class UserActionQueue
{
    private readonly Channel<UserEvent> _queue;
    private const int QUEUE_CHANNEL_CAPACITY = 100;

    /// <summary>Initializes a new instance of the <see cref="UserActionQueue"/> class.</summary>
    public UserActionQueue() {
        _queue = Channel.CreateBounded<UserEvent>(new BoundedChannelOptions(QUEUE_CHANNEL_CAPACITY) {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }
    /// <summary>Gets the reader for the queue.</summary>
    public ChannelReader<UserEvent> Reader => _queue.Reader;

    /// <summary>Gets the writer for the queue.</summary>
    public ValueTask EnqueueAsync(UserEvent lastEvent) {
        return _queue.Writer.WriteAsync(lastEvent);
    }
}
