using System.Threading.Channels;
using Indice.Features.ActivityLogs.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.ActivityLogs;

internal class ActivityLogEntryQueue
{
    private readonly Channel<ActivityLogEntry> _queue;
    private readonly ActivityLogOptions _ActivityLogOptions;

    public ActivityLogEntryQueue(IOptions<ActivityLogOptions> ActivityLogOptions) {
        _ActivityLogOptions = ActivityLogOptions?.Value ?? throw new ArgumentNullException(nameof(ActivityLogOptions));
        _queue = Channel.CreateBounded<ActivityLogEntry>(new BoundedChannelOptions(_ActivityLogOptions.QueueChannelCapacity) {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public ChannelReader<ActivityLogEntry> Reader => _queue.Reader;

    public ValueTask EnqueueAsync(ActivityLogEntry logEntry) {
        return _queue.Writer.WriteAsync(logEntry);
    }
}
