using FluentValidation;
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
    /// <exception cref="ArgumentNullException"></exception>
    public ProfileInputModelValidator(
        IStringLocalizer<ProfileInputModelValidator> localizer,
        IOptionsSnapshot<IdentityOptions> identityOptions
    ) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        RuleFor(x => x.UserName).NotEmpty().WithName(_localizer["Username"]);
        RuleFor(x => x.UserName).UserName(identityOptions.Value.User).WithName(_localizer["Username"]).WithMessage(_localizer["Field '{PropertyName}' can accept digits, uppercase or lowercase latin characters and the symbols -._@+"]);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.PhoneNumber).Length(10).WithName(_localizer["Mobile phone"]);
        RuleFor(x => x.PhoneNumber).Must(phoneNumber => string.IsNullOrEmpty(phoneNumber) || phoneNumber.StartsWith("69")).WithMessage(_localizer["Mobile phone must start with '69' and have 10 digits."]);
        RuleFor(x => x.Tin).TaxCode("GR").WithName(_localizer["Tin"]).WithMessage(_localizer["Invalid Tax Code."]);
    }
}
