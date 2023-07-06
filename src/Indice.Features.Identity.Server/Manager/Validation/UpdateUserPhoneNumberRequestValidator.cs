using FluentValidation;
using Indice.Features.Identity.Server.Manager.Models;

namespace Indice.Features.Identity.Server.Manager.Validation;

/// <summary></summary>
public class UpdateUserPhoneNumberRequestValidator : AbstractValidator<UpdateUserPhoneNumberRequest>
{
    /// <summary></summary>
    public UpdateUserPhoneNumberRequestValidator() {
        RuleFor(x => x.PhoneNumber).NotEmpty();
    }
}
