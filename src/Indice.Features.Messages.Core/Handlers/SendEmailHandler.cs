using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Indice.Types;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>Job handler for <see cref="SendEmailEvent"/>.</summary>
public class SendEmailHandler : ICampaignJobHandler<SendEmailEvent>
{
    /// <summary>Creates a new instance of <see cref="SendEmailHandler"/>.</summary>
    /// <param name="emailService">Push notification service abstraction in order to support different providers.</param>
    /// <param name="campaignAttachmentService">A service that contains campaign attachments related operations.</param>
    /// <param name="messageSenderService">A service that contains message sender related operations.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SendEmailHandler(IEmailService emailService, ICampaignAttachmentService campaignAttachmentService, IMessageSenderService messageSenderService) {
        EmailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        CampaignAttachmentService = campaignAttachmentService ?? throw new ArgumentNullException(nameof(campaignAttachmentService));
        MessageSenderService = messageSenderService ?? throw new ArgumentNullException(nameof(messageSenderService));
    }

    private ICampaignAttachmentService CampaignAttachmentService { get; }
    private IMessageSenderService MessageSenderService { get; }
    private IEmailService EmailService { get; }

    /// <summary>Sends an email to a single user.</summary>
    /// <param name="event">The event model used when sending an email.</param>
    public async Task Process(SendEmailEvent @event) {
        var attachment = await CampaignAttachmentService.GetFile(@event.CampaignId, Guid.NewGuid());
        var sender = @event.Sender;
        if (sender is null || sender.IsEmpty) {
            var defaultSenderResult = await MessageSenderService.GetList(new ListOptions { Page = 1, Size = 1 }, new MessageSenderListFilter {
                IsDefault = true
            });
            sender = defaultSenderResult?.Items?.FirstOrDefault();
        }
        await EmailService.SendAsync(builder => {
            if (sender is not null && !sender.IsEmpty) {
                builder.From(sender.Sender!, sender.DisplayName);
            }
            builder.To(@event.RecipientEmail!)
                   .WithSubject(@event.Title!)
                   .WithBody(@event.Body!);
            if (attachment is not null) {
                builder.WithAttachments(new EmailAttachment(attachment.Name!, attachment.Data!));
            }
        });
    }
}
