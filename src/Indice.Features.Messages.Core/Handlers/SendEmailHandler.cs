using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>Job handler for <see cref="SendEmailEvent"/>.</summary>
public class SendEmailHandler : ICampaignJobHandler<SendEmailEvent>
{
    /// <summary>Creates a new instance of <see cref="SendEmailHandler"/>.</summary>
    /// <param name="emailService">Push notification service abstraction in order to support different providers.</param>
    /// <param name="campaignAttachmentService">A service that contains campaign attachments related operations.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SendEmailHandler(IEmailService emailService, ICampaignAttachmentService campaignAttachmentService) {
        EmailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        CampaignAttachmentService = campaignAttachmentService ?? throw new ArgumentNullException(nameof(campaignAttachmentService));
    }

    private ICampaignAttachmentService CampaignAttachmentService { get; }
    private IEmailService EmailService { get; }

    /// <summary>Sends an email to a single user.</summary>
    /// <param name="event">The event model used when sending an email.</param>
    public async Task Process(SendEmailEvent @event) {
        var attachment = await CampaignAttachmentService.GetFile(@event.CampaignId, Guid.NewGuid());
        await EmailService.SendAsync(builder => {
            if (@event.Sender is not null && !@event.Sender.IsEmpty) {
                builder.From(@event.Sender.Sender, @event.Sender.DisplayName);
            }
            builder.To(@event.RecipientEmail)
                   .WithSubject(@event.Title)
                   .WithBody(@event.Body);
            if (attachment is not null) {
                builder.WithAttachments(new EmailAttachment(attachment.Name, attachment.Data));
            }
        });
    }
}
