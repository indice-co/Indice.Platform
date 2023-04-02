using System.Threading.Channels;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs;

internal class SignInLogEntryQueue
{
    private readonly Channel<SignInLogEntry> _queue;

    public SignInLogEntryQueue() => 
        _queue = Channel.CreateUnbounded<SignInLogEntry>(new UnboundedChannelOptions {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false
        });

    public async Task EnqueueAsync(SignInLogEntry LogEntry) => await _queue.Writer.WriteAsync(LogEntry);

    public bool TryDequeue(out SignInLogEntry logEntry) {
        var isRead = _queue.Reader.TryRead(out var dequeuedLogEntry);
        logEntry = dequeuedLogEntry;
        return isRead;
    }
}
