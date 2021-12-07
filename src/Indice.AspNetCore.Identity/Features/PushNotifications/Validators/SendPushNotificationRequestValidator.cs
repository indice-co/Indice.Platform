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
            RuleFor(x => x.Message).NotEmpty().WithMessage("Please provide a message.");
            RuleFor(x => x.UserCode).NotEmpty().When(x => !x.Broadcast).WithMessage("Please provide a user code.");
        }
    }
}
