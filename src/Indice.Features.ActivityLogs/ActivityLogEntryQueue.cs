using System.Threading.Channels;
using Indice.Features.ActivityLogs.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.ActivityLogs;

/// <summary>
/// Activity log entry queue used to enqueue activity log entries for processing by a background service.
/// </summary>
public class ActivityLogEntryQueue
{
    private readonly Channel<ActivityLogEntry> _queue;
    private readonly ActivityLogOptions _ActivityLogOptions;

    /// <summary>
    /// Initializes a new instance of the ActivityLogEntryQueue class using the specified activity log options.
    /// </summary>
    /// <param name="ActivityLogOptions"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ActivityLogEntryQueue(IOptions<ActivityLogOptions> ActivityLogOptions) {
        _ActivityLogOptions = ActivityLogOptions?.Value ?? throw new ArgumentNullException(nameof(ActivityLogOptions));
        _queue = Channel.CreateBounded<ActivityLogEntry>(new BoundedChannelOptions(_ActivityLogOptions.QueueChannelCapacity) {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }
    /// <summary>
    /// Gets the channel reader used to read activity log entries from the queue.
    /// </summary>
    public ChannelReader<ActivityLogEntry> Reader => _queue.Reader;

    /// <summary>
    /// Asynchronously enqueues an activity log entry for processing.   
    /// </summary>
    /// <param name="logEntry">The activity log entry to add to the queue. Cannot be null.</param>
    /// <returns>A ValueTask that represents the asynchronous enqueue operation.</returns>
    public ValueTask EnqueueAsync(ActivityLogEntry logEntry) {
        return _queue.Writer.WriteAsync(logEntry);
    }
}
