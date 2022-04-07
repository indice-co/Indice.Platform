using Indice.Features.Messages.Core.Events;
using Indice.Services;

namespace Indice.Features.Messages.Core.Handlers
{
    /// <summary>
    /// Job handler for <see cref="SendEmailEvent"/>.
    /// </summary>
    public class SendEmailHandler : ICampaignJobHandler<SendEmailEvent>
    {
        /// <summary>
        /// Creates a new instance of <see cref="SendEmailHandler"/>.
        /// </summary>
        /// <param name="emailService">Push notification service abstraction in order to support different providers.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public SendEmailHandler(IEmailService emailService) {
            EmailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        private IEmailService EmailService { get; }

        /// <summary>
        /// Sends a push notification to a single user.
        /// </summary>
        /// <param name="event">The event model used when sending an email.</param>
        public Task Process(SendEmailEvent @event) {
            throw new NotImplementedException();
        }
    }
}
