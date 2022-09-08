using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Worker.Handlers
{
    internal class SendPushNotificationJobHandler
    {
        public SendPushNotificationJobHandler(
            ILogger<SendPushNotificationJobHandler> logger,
            MessageJobHandlerFactory messageJobHandlerFactory
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            MessageJobHandlerFactory = messageJobHandlerFactory ?? throw new ArgumentNullException(nameof(messageJobHandlerFactory));
        }

        public ILogger<SendPushNotificationJobHandler> Logger { get; }
        public MessageJobHandlerFactory MessageJobHandlerFactory { get; }

        public async Task Process(SendPushNotificationEvent pushNotification) {
            var handler = MessageJobHandlerFactory.CreateFor<SendPushNotificationEvent>();
            await handler.Process(pushNotification);
        }
    }
}
