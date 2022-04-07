using Indice.Features.Messages.Core.Events;
using Indice.Services;

namespace Indice.Features.Messages.Core.Handlers
{
    /// <summary>
    /// Job handler for <see cref="SendSmsEvent"/>.
    /// </summary>
    public class SendSmsHandler : ICampaignJobHandler<SendSmsEvent>
    {
        /// <summary>
        /// Creates a new instance of <see cref="SendEmailHandler"/>.
        /// </summary>
        /// <param name="smsService">Push notification service abstraction in order to support different providers.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public SendSmsHandler(ISmsService smsService) {
            SmsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        }

        private ISmsService SmsService { get; }

        /// <summary>
        /// Sends a push notification to a single user.
        /// </summary>
        /// <param name="event">The event model used when sending an email.</param>
        public Task Process(SendSmsEvent @event) {
            throw new NotImplementedException();
        }
    }
}
