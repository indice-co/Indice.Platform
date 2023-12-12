#if !NETSTANDARD2_1
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Indice.Events;

internal class BackgroundPlatformEventServiceQueue<TEvent> where TEvent : IPlatformEvent
{
    private readonly Channel<TEvent> _queue;
    private readonly BackgroundPlatformEventServiceQueueOptions _queueOptions;

    public BackgroundPlatformEventServiceQueue(IOptions<BackgroundPlatformEventServiceQueueOptions> signInLogOptions) {
        _queueOptions = signInLogOptions?.Value ?? throw new ArgumentNullException(nameof(signInLogOptions));
        _queue = Channel.CreateBounded<TEvent>(new BoundedChannelOptions(_queueOptions.QueueChannelCapacity) {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public ChannelReader<TEvent> Reader => _queue.Reader;

    public ValueTask EnqueueAsync(TEvent @event) => _queue.Writer.WriteAsync(@event);
}

internal class BackgroundPlatformEventServiceQueueOptions 
{
    /// <summary>The maximum number of items the internal queue may store. Defaults to <i>100</i>.</summary>
    public int QueueChannelCapacity { get; set; } = 100;
    /// <summary>The number of items the dequeue batch contains. Should be extra careful when configuring!!!</summary>
    public int DequeueBatchSize { get; set; } = 10;
    /// <summary>The timeout milliseconds the queue waits to reach the batch size. Should be extra careful when configuring!!!</summary>
    public long DequeueTimeoutInMilliseconds { get; set; } = 1000;
}
#endif
