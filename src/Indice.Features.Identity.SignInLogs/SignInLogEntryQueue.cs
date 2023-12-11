using System.Threading.Channels;
using Indice.Features.Identity.SignInLogs.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.SignInLogs;

internal class SignInLogEntryQueue
{
    private readonly Channel<SignInLogEntry> _queue;
    private readonly SignInLogOptions _signInLogOptions;

    public SignInLogEntryQueue(IOptions<SignInLogOptions> signInLogOptions) {
        _signInLogOptions = signInLogOptions?.Value ?? throw new ArgumentNullException(nameof(signInLogOptions));
        _queue = Channel.CreateBounded<SignInLogEntry>(new BoundedChannelOptions(_signInLogOptions.QueueChannelCapacity) {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public ChannelReader<SignInLogEntry> Reader => _queue.Reader;

    public ValueTask EnqueueAsync(SignInLogEntry logEntry) {
        var skipEvent = (!_signInLogOptions.ImpossibleTravel.RecordTokenEvents && logEntry.EventType == SignInLogEventType.TokenIssued) ||
                        (!_signInLogOptions.ImpossibleTravel.RecordPasswordEvents && logEntry.EventType == SignInLogEventType.UserPasswordValidationCompleted);
        return skipEvent ? ValueTask.CompletedTask : _queue.Writer.WriteAsync(logEntry);
    }
}
