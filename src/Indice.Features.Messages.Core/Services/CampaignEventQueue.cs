using System.Threading.Channels;
using Indice.Features.Messages.Core.Events;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services;
/// <summary>Queue for storing campaign events.</summary>
public class CampaignEventQueue
{
    private readonly CampaignStatisticOptions _statisticOptions;
    private readonly Channel<MessageEvent> _queue;
    private const int QUEUE_CHANNEL_CAPACITY = 100;

    /// <summary>Initializes a new instance of the <see cref="CampaignEventQueue"/> class.</summary>
    /// <param name="statisticOptions">Configuration for campaign statistics feature.</param>   
    public CampaignEventQueue(IOptions<CampaignStatisticOptions> statisticOptions) {
        _statisticOptions = statisticOptions.Value;
        _queue = Channel.CreateBounded<MessageEvent>(new BoundedChannelOptions(QUEUE_CHANNEL_CAPACITY) {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }
    /// <summary>Gets the reader for the queue.</summary>
    public ChannelReader<MessageEvent> Reader => _queue.Reader;

    /// <summary>Gets the writer for the queue.</summary>
    public ValueTask EnqueueAsync(MessageEvent lastActivityEntry) {
        if (!_statisticOptions.EnableStatics) return ValueTask.CompletedTask;
        return _queue.Writer.WriteAsync(lastActivityEntry);
    }
}
