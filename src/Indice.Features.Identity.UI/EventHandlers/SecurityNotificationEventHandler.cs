using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.SignInLogs.Events;
using Indice.Localization;
using Indice.Services;

namespace Indice.Features.Identity.UI.EventHandlers;

/// <summary>
/// Handles security notification events by processing the event data and sending notifications.
/// </summary>
/// <remarks>This class is responsible for handling <see cref="SecurityNotificationEvent"/> instances and
/// performing necessary actions, such as sending email notifications using the provided <see
/// cref="IEmailService"/>.</remarks>
public class SecurityNotificationEventHandler : IPlatformEventHandler<SecurityNotificationEvent>
{
    private readonly IEmailService _emailService;
    private readonly IdentityMessageDescriber _messageDescriber;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityNotificationEventHandler"/> class.
    /// </summary>
    /// <param name="emailService">The email service used to send security notifications. This parameter cannot be <see langword="null"/>.</param>
    /// <param name="messageDescriber">Provides the various messages used throughout Indice packages.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="emailService"/> is <see langword="null"/>.</exception>


    public SecurityNotificationEventHandler(IEmailService emailService, IdentityMessageDescriber messageDescriber) {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _messageDescriber = messageDescriber;
    }

    /// <inheritdoc/>
    public async Task Handle(SecurityNotificationEvent @event, PlatformEventArgs args) {
        if (string.IsNullOrWhiteSpace(@event.User.Email)) {
            return; // No email to send notification to.
        }
        using (new TemporaryCulture(@event.Locale)) {
            var subject = _messageDescriber.SecurityEventSubject(@event.Activity);
            var description = _messageDescriber.SecurityEventDescription(@event.Activity);
            await _emailService.SendAsync(email => {
                email.To(@event.User.Email)
                    .WithSubject(subject)
                    .WithData(new SecurityNotificationModel {
                        User = @event.User,
                        Location = @event.Location,
                        TimeStamp = @event.TimeStamp,
                        Client = @event.Client,
                        Device = @event.Device,
                        DisplayName = @event.User.UserName,
                        Subject = subject,
                        Description = description,
                    })
                    .UsingTemplate("EmailSecurityNotification");
            });

        }
    }
}
