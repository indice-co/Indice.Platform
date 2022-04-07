using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Worker.Handlers
{
    internal class SendSmsJobHandler
    {
        public SendSmsJobHandler(
            ILogger<SendSmsJobHandler> logger,
            MessageJobHandlerFactory messageJobHandlerFactory
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            MessageJobHandlerFactory = messageJobHandlerFactory ?? throw new ArgumentNullException(nameof(messageJobHandlerFactory));
        }

        public ILogger<SendSmsJobHandler> Logger { get; }
        public MessageJobHandlerFactory MessageJobHandlerFactory { get; }

        public async Task Process(SendSmsEvent sms) {
            var handler = MessageJobHandlerFactory.Create<SendSmsEvent>();
            await handler.Process(sms);
        }
    }
}
