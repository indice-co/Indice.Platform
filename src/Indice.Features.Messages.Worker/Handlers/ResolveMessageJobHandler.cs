using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Worker.Handlers
{
    internal class ResolveMessageJobHandler
    {
        public ResolveMessageJobHandler(
            ILogger<ResolveMessageJobHandler> logger,
            MessageJobHandlerFactory messageJobHandlerFactory
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            MessageJobHandlerFactory = messageJobHandlerFactory;
        }

        public ILogger<ResolveMessageJobHandler> Logger { get; }
        public MessageJobHandlerFactory MessageJobHandlerFactory { get; }

        public async Task Process(ResolveMessageEvent @event) {
            var handler = MessageJobHandlerFactory.CreateFor<ResolveMessageEvent>();
            await handler.Process(@event);
        }
    }
}
