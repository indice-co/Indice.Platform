using FluentValidation;

namespace Indice.AspNetCore.Identity.Api.Models.Validators
{
    /// <summary>
    /// A validator for <see cref="SendPushNotificationRequestValidator"/> model.
    /// </summary>
    public class SendPushNotificationRequestValidator : AbstractValidator<SendPushNotificationRequest>
    {
        /// <summary>
        /// Creates a new instance of <see cref="SendPushNotificationRequestValidator"/>.
        /// </summary>
        public SendPushNotificationRequestValidator() {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Please provide a message.");
            RuleFor(x => x.UserTag).NotEmpty().When(x => !x.Broadcast).WithMessage("Please provide a user code.");
        }
    }
}
