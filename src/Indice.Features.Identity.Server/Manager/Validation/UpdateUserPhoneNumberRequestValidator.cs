using FluentValidation;
using Indice.Features.Identity.Core.PhoneNumberValidation;
using Indice.Features.Identity.Server.Manager.Models;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Server.Manager.Validation;

/// <summary></summary>
public class UpdateUserPhoneNumberRequestValidator : AbstractValidator<UpdateUserPhoneNumberRequest>
{
    private readonly IConfiguration _configuration;

    /// <summary></summary>
    public UpdateUserPhoneNumberRequestValidator(IConfiguration configuration, IPhoneNumberValidator phoneNumberValidator) {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        var regexMessage = configuration.GetIdentityOption("User", "PhoneNumberRegexMessage");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(phoneNumberValidator.Validate)
            .WithMessage(string.IsNullOrWhiteSpace(regexMessage) ? "The field '{PropertyName}' has invalid format." : regexMessage);
    }
}
