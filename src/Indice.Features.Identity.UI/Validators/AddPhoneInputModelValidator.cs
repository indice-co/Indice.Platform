using FluentValidation;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="AddPhoneInputModel"/> class.</summary>
public class AddPhoneInputModelValidator : AbstractValidator<AddPhoneInputModel>
{
    private readonly IStringLocalizer<AddPhoneInputModelValidator> _localizer;
    private readonly IConfiguration _configuration;

    /// <summary>Creates a new instance of <see cref="AddPhoneInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <param name="configuration">Represents the configuration element.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AddPhoneInputModelValidator(IStringLocalizer<AddPhoneInputModelValidator> localizer, IConfiguration configuration) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        RuleFor(x => x.PhoneNumber).NotEmpty().WithName(_localizer["Phone Number"]).UserPhoneNumber(_configuration).WithMessage(_localizer["The field '{PropertyName}' has invalid format."]);
    }
}
