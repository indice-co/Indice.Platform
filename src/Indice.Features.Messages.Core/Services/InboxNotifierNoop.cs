using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services.Abstractions;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="IInboxNotifier"/> that does nothing.</summary>
public class InboxNotifierNoop : IInboxNotifier
{
    /// <inheritdoc />
    public Task NotifyAllAsync(InboxMessageNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task NotifyUserAsync(string recipientId, InboxMessageNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
