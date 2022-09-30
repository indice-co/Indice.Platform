using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;

namespace Indice.Features.Messages.Core.Handlers
{
    /// <summary>
    /// Job handler for <see cref="SendEmailEvent"/>.
    /// </summary>
    public class SendEmailHandler : ICampaignJobHandler<SendEmailEvent>
    {
        /// <inheritdoc/>
        public SendEmailHandler(ICampaignAttachmentService campaignAttachmentService) {
            CampaignAttachmentService = campaignAttachmentService;
        }

        /// <summary>
        /// Creates a new instance of <see cref="SendEmailHandler"/>.
        /// </summary>
        /// <param name="emailService">Push notification service abstraction in order to support different providers.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public SendEmailHandler(IEmailService emailService) {
            EmailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        private ICampaignAttachmentService CampaignAttachmentService { get; }
        private IEmailService EmailService { get; }

        /// <summary>
        /// Sends a push notification to a single user.
        /// </summary>
        /// <param name="event">The event model used when sending an email.</param>
        public async Task Process(SendEmailEvent @event) {

            Action<EmailMessageBuilder> configure = message => message
            .To(@event.RecipientEmail)
            .WithSubject(@event.Title)
            .WithBody(@event.Body);

            var attachment = await CampaignAttachmentService.GetFile(@event.CampaignId, Guid.NewGuid());
            if (attachment is not null) {
                configure = message => message
                .To(@event.RecipientEmail)
                .WithSubject(@event.Title)
                .WithBody(@event.Body)
                .WithAttachments(new EmailAttachment(attachment.Name, attachment.Data));
            }
            await EmailService.SendAsync(configure);
        }

    }
}
