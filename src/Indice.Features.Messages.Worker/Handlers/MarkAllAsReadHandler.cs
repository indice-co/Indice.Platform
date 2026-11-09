using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Worker.Handlers;

internal class MarkAllAsReadHandler
{
    public MarkAllAsReadHandler(
        ILogger<MarkAllAsReadHandler> logger,
        MessageJobHandlerFactory messageJobHandlerFactory
    ) {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        MessageJobHandlerFactory = messageJobHandlerFactory ?? throw new ArgumentNullException(nameof(messageJobHandlerFactory));
    }

    public ILogger<MarkAllAsReadHandler> Logger { get; }
    public MessageJobHandlerFactory MessageJobHandlerFactory { get; }

    public async Task Process(MarkMessagesReadEvent @event) {
        var handler = MessageJobHandlerFactory.CreateFor<MarkMessagesReadEvent>();
        await handler.Process(@event);
    }
}
