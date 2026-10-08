using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Worker.Handlers;

internal class MarkAllAsUnreadHandler
{
    public MarkAllAsUnreadHandler(
        ILogger<MarkAllAsUnreadHandler> logger,
        MessageJobHandlerFactory messageJobHandlerFactory
    ) {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        MessageJobHandlerFactory = messageJobHandlerFactory ?? throw new ArgumentNullException(nameof(messageJobHandlerFactory));
    }

    public ILogger<MarkAllAsUnreadHandler> Logger { get; }
    public MessageJobHandlerFactory MessageJobHandlerFactory { get; }

    public async Task Process(MarkMessagesUnreadEvent @event) {
        var handler = MessageJobHandlerFactory.CreateFor<MarkMessagesUnreadEvent>();
        await handler.Process(@event);
    }
}
