using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Worker.Handlers;

internal class DatabaseCleanUpJobHandler
{
    public DatabaseCleanUpJobHandler(
        ILogger<DatabaseCleanUpJobHandler> logger,
        MessageJobHandlerFactory messageJobHandlerFactory
    ) {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        MessageJobHandlerFactory = messageJobHandlerFactory;
    }

    public ILogger<DatabaseCleanUpJobHandler> Logger { get; }
    public MessageJobHandlerFactory MessageJobHandlerFactory { get; }

    public async Task Process(DatabaseCleanUpTimerEvent @event) {
        var handler = MessageJobHandlerFactory.CreateFor<DatabaseCleanUpTimerEvent>();
        await handler.Process(@event);
    }
}
