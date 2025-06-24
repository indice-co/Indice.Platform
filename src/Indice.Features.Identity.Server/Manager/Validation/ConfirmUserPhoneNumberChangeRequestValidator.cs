using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Server.Manager.Models;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Server.Manager.Validation;

/// <summary>
/// Validates the <see cref="ChangeUserPhoneNumberRequestValidator"/> model.
/// </summary>
public class ConfirmUserPhoneNumberChangeRequestValidator : AbstractValidator<ConfirmPhoneNumberChangeRequest>
{
    private readonly IConfiguration _configuration;

    /// <summary>Constructor that configures the rules</summary>
    public ConfirmUserPhoneNumberChangeRequestValidator(IConfiguration configuration, CallingCodesProvider callingCodesProvider) {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        RuleFor(x => x.PhoneNumber).UserPhoneNumber(_configuration, callingCodesProvider).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
    }
}
