using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Worker.Handlers;

internal class MessagingDatabaseCleanUpJobHandler
{
    public MessagingDatabaseCleanUpJobHandler(
        ILogger<MessagingDatabaseCleanUpJobHandler> logger,
        MessageJobHandlerFactory messageJobHandlerFactory
    ) {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        MessageJobHandlerFactory = messageJobHandlerFactory;
    }

    public ILogger<MessagingDatabaseCleanUpJobHandler> Logger { get; }
    public MessageJobHandlerFactory MessageJobHandlerFactory { get; }

    public async Task Process(MessagingDatabaseCleanUpTimerEvent @event) {
        var handler = MessageJobHandlerFactory.CreateFor<MessagingDatabaseCleanUpTimerEvent>();
        await handler.Process(@event);
    }
}
