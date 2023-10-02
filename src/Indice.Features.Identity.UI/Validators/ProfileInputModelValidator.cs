using FluentValidation;
using Indice.Features.Identity.Core.PhoneNumberValidation;
using Indice.Features.Identity.UI.Models;
using Indice.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="ProfileInputModelValidator"/> class.</summary>
public class ProfileInputModelValidator : AbstractValidator<ProfileInputModel>
{
    private readonly IStringLocalizer<ProfileInputModelValidator> _localizer;

    /// <summary>Creates a new instance of <see cref="ProfileInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <param name="identityOptions">Represents all the options you can use to configure the identity system.</param>
    /// <param name="phoneNumberValidator">Represents a validator that validates phone numbers.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ProfileInputModelValidator(
        IStringLocalizer<ProfileInputModelValidator> localizer,
        IOptionsSnapshot<IdentityOptions> identityOptions,
        IPhoneNumberValidator phoneNumberValidator
    ) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        ArgumentNullException.ThrowIfNull(nameof(phoneNumberValidator));

        RuleFor(x => x.UserName).NotEmpty().WithName(_localizer["Username"]);
        RuleFor(x => x.UserName).UserName(identityOptions.Value.User).WithName(_localizer["Username"]).WithMessage(_localizer["Field '{PropertyName}' can accept digits, uppercase or lowercase latin characters and the symbols -._@+"]);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithName(_localizer["Mobile phone"])
            .Must((model, phone) => phoneNumberValidator.Validate($"{model.PhoneCallingCode}{phone}"))
            .WithMessage(_localizer["The field '{PropertyName}' has invalid format."]);
        RuleFor(x => x.Tin).TaxCode("GR").WithName(_localizer["Tin"]).WithMessage(_localizer["Invalid Tax Code."]);
    }
}
