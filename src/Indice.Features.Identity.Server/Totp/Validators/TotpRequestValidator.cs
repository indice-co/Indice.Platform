using FluentValidation;
using Indice.Features.Identity.Server.Totp.Models;
using Indice.Services;

namespace Indice.Features.Identity.Server.Totp.Validators;

internal class TotpRequestValidator : AbstractValidator<TotpRequest>
{
    public TotpRequestValidator() {
        RuleFor(x => x.Message).NotEmpty();
        RuleFor(x => x.AuthenticationMethod)
            .NotEmpty()
            .When(x => !x.Channel.HasValue)
            .WithMessage($"Please set either '{nameof(TotpRequest.AuthenticationMethod)}' or '{nameof(TotpRequest.Channel)}' property.");
        RuleFor(x => x.Channel)
            .NotEmpty()
            .When(x => string.IsNullOrWhiteSpace(x.AuthenticationMethod))
            .WithMessage($"Please set either '{nameof(TotpRequest.AuthenticationMethod)}' or '{nameof(TotpRequest.Channel)}' property.");
        RuleFor(x => x.EmailTemplate)
            .NotEmpty()
            .When(x => x.Channel.HasValue && x.Channel.Value == TotpDeliveryChannel.Email);
    }
}
