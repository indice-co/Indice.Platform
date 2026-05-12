using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Models;
using Indice.Localization;
using Indice.Services;

namespace Indice.Features.Identity.UI.EventHandlers;

/// <summary>Handles <see cref="TwoFactorPreferenceChangedEvent"/> by sending an email notification to the user.</summary>
public class TwoFactorPreferenceChangedEventHandler : IPlatformEventHandler<TwoFactorPreferenceChangedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IdentityMessageDescriber _messageDescriber;

    /// <summary>Initializes a new instance of the <see cref="TwoFactorPreferenceChangedEventHandler"/> class.</summary>
    /// <param name="emailService">The email service used to send notifications. Cannot be <see langword="null"/>.</param>
    /// <param name="messageDescriber">Provides the various messages used throughout Indice packages.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="emailService"/> is <see langword="null"/>.</exception>
    public TwoFactorPreferenceChangedEventHandler(IEmailService emailService, IdentityMessageDescriber messageDescriber) {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _messageDescriber = messageDescriber;
    }

    /// <inheritdoc/>
    public async Task Handle(TwoFactorPreferenceChangedEvent @event, PlatformEventArgs args) {
        if (string.IsNullOrWhiteSpace(@event.User.Email)) {
            return; // No email to send notification to.
        }
        var subject = _messageDescriber.TwoFactorPreferenceChangedEventSubject;
        var description = _messageDescriber.TwoFactorPreferenceChangedEventDescription(@event.AuthenticationMethodCode);
        await _emailService.SendAsync(email => {
            email.To(@event.User.Email)
                .WithSubject(subject)
                .WithData(new TwoFactorPreferenceChangedNotificationModel {
                    User = @event.User,
                    DisplayName = @event.User.UserName,
                    AuthenticationMethodCode = @event.AuthenticationMethodCode,
                    TimeStamp = DateTimeOffset.UtcNow,
                    Subject = subject,
                    Description = description,
                })
                .UsingTemplate("EmailTwoFactorPreferenceChanged");
        });
    }
}
