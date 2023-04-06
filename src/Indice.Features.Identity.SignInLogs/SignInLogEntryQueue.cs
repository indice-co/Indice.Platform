using System.Threading.Channels;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs;

internal class SignInLogEntryQueue
{
    private readonly Channel<SignInLogEntry> _queue;

    public SignInLogEntryQueue() =>
        _queue = Channel.CreateBounded<SignInLogEntry>(new BoundedChannelOptions(100) {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    public ValueTask EnqueueAsync(SignInLogEntry logEntry) => _queue.Writer.WriteAsync(logEntry);

    public IAsyncEnumerable<SignInLogEntry> DequeueAllAsync() => _queue.Reader.ReadAllAsync();
}
