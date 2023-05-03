using FluentValidation;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="RegisterInputModel"/> class.</summary>
public class RegisterInputModelValidator : AbstractValidator<RegisterInputModel>
{
    private readonly IStringLocalizer<RegisterInputModelValidator> _localizer;
    private readonly ExtendedIdentityDbContext<User, Role> _dbContext;

    /// <summary>Creates a new instance of <see cref="LoginInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <param name="dbContext">An extended <see cref="DbContext"/> for the Identity framework.</param>
    /// <param name="identityOptions">Represents all the options you can use to configure the identity system.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public RegisterInputModelValidator(
        IStringLocalizer<RegisterInputModelValidator> localizer,
        ExtendedIdentityDbContext<User, Role> dbContext,
        IOptionsSnapshot<IdentityOptions> identityOptions
    ) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        RuleFor(x => x.FirstName).NotEmpty().WithName(_localizer["First name"]);
        RuleFor(x => x.LastName).NotEmpty().WithName(_localizer["Last name"]);
        RuleFor(x => x.UserName).NotEmpty().WithName(_localizer["Username"]);
        RuleFor(x => x.UserName).UserName(identityOptions.Value.User).WithName(_localizer["Username"]).WithMessage(_localizer["Field '{PropertyName}' can accept digits, uppercase or lowercase latin characters and the symbols -._@+"]);
        RuleFor(x => x.UserName).Must(UserNameNotBeAssignedToAnotherUser).WithMessage(_localizer["This username already exists. Please use a different one."]);
        RuleFor(x => x.Password).NotEmpty().WithName(_localizer["Password"]);
        RuleFor(x => x.PhoneNumber).Length(10).WithName(_localizer["Mobile phone"]);
        RuleFor(x => x.PhoneNumber).Must(p => string.IsNullOrEmpty(p) || p.StartsWith("69")).WithMessage(_localizer["Mobile phone must start with '69' and have 10 digits."]);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Email).Must(EmailNotBeAssignedToAnotherUser).WithMessage(_localizer["This email already exists. Please use a different email."]);
        RuleFor(x => x.HasAcceptedTerms).Equal(true).WithMessage(_localizer["You must accept the service 'terms of use'."]);
        RuleFor(x => x.HasReadPrivacyPolicy).Equal(true).WithMessage(_localizer["You must be informed about privacy policy."]);
    }

    private bool EmailNotBeAssignedToAnotherUser(string? email) => !string.IsNullOrWhiteSpace(email) && !_dbContext.Users.Any(x => x.Email == email);

    private bool UserNameNotBeAssignedToAnotherUser(string? userΝame) => !string.IsNullOrWhiteSpace(userΝame) && !_dbContext.Users.Any(x => x.UserName == userΝame);
}
