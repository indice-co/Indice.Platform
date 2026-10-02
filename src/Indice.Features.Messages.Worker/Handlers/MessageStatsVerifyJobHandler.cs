using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Worker.Handlers;

internal class MessageStatsVerifyJobHandler
{
    public MessageStatsVerifyJobHandler(
        ILogger<MessageStatsVerifyJobHandler> logger,
        MessageJobHandlerFactory messageJobHandlerFactory
    ) {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        MessageJobHandlerFactory = messageJobHandlerFactory;
    }

    public ILogger<MessageStatsVerifyJobHandler> Logger { get; }
    public MessageJobHandlerFactory MessageJobHandlerFactory { get; }

    // A scheduled job has no work item, so the event is created here. The worker host resolves every other parameter from the service provider.
    public async Task Process() {
        var handler = MessageJobHandlerFactory.CreateFor<MessageStatsVerifyTimerEvent>();
        await handler.Process(new MessageStatsVerifyTimerEvent());
    }
}
