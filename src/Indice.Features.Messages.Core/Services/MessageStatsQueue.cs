using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services;
/// <summary>Queue for storing the successful sends that wait to be added to the send statistics.</summary>
/// <remarks>The queue never makes the writer wait. When it is full the new item is dropped and counted.</remarks>
public class MessageStatsQueue
{
    private readonly Channel<MessageStatDelta> _queue;
    private long _droppedCount;

    /// <summary>Initializes a new instance of the <see cref="MessageStatsQueue"/> class.</summary>
    /// <param name="analyticsOptions">Configuration for the analytics feature.</param>
    public MessageStatsQueue(IOptions<AnalyticsOptions> analyticsOptions) {
        _queue = Channel.CreateBounded<MessageStatDelta>(new BoundedChannelOptions(analyticsOptions.Value.Stats.ChannelCapacity) {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            // With Wait, TryWrite returns false when the queue is full. DropWrite would return true and hide the drop.
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    /// <summary>Gets the reader for the queue.</summary>
    public ChannelReader<MessageStatDelta> Reader => _queue.Reader;
    /// <summary>The number of items dropped because the queue was full.</summary>
    public long DroppedCount => Interlocked.Read(ref _droppedCount);

    /// <summary>Adds an item to the queue, or drops it when the queue is full.</summary>
    /// <param name="delta">The successful send.</param>
    /// <returns>False when the item was dropped.</returns>
    public bool TryEnqueue(MessageStatDelta delta) {
        if (_queue.Writer.TryWrite(delta)) {
            return true;
        }
        Interlocked.Increment(ref _droppedCount);
        return false;
    }
}
