using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Worker.Handlers
{
    internal class SendEmailJobHandler
    {
        public SendEmailJobHandler(
            ILogger<SendEmailJobHandler> logger,
            MessageJobHandlerFactory messageJobHandlerFactory
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            MessageJobHandlerFactory = messageJobHandlerFactory ?? throw new ArgumentNullException(nameof(messageJobHandlerFactory));
        }

        public ILogger<SendEmailJobHandler> Logger { get; }
        public MessageJobHandlerFactory MessageJobHandlerFactory { get; }

        public async Task Process(SendEmailEvent email) {
            var handler = MessageJobHandlerFactory.Create<SendEmailEvent>();
            await handler.Process(email);
        }
    }
}
