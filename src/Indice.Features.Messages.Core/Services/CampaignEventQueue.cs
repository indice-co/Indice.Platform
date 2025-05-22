using System.Threading.Channels;
using Indice.Features.Messages.Core.Events;

namespace Indice.Features.Messages.Core.Services;
/// <summary>Queue for storing campaign events.</summary>
public class CampaignEventQueue
{
    private readonly Channel<CampaignEvent> _queue;
    private const int QUEUE_CHANNEL_CAPACITY = 100;

    /// <summary>Initializes a new instance of the <see cref="CampaignEventQueue"/> class.</summary>
    public CampaignEventQueue() {
        _queue = Channel.CreateBounded<CampaignEvent>(new BoundedChannelOptions(QUEUE_CHANNEL_CAPACITY) {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }
    /// <summary>Gets the reader for the queue.</summary>
    public ChannelReader<CampaignEvent> Reader => _queue.Reader;

    /// <summary>Gets the writer for the queue.</summary>
    public ValueTask EnqueueAsync(CampaignEvent lastActivityEntry) {
        return _queue.Writer.WriteAsync(lastActivityEntry);
    }
}
