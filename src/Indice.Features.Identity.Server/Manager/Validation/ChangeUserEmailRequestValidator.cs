using FluentValidation;
using Indice.Features.Identity.Server.Manager.Models;

namespace Indice.Features.Identity.Server.Manager.Validation;
/// <summary>
/// Provides validation rules for the <see cref="ChangeUserEmailRequest"/> object.
/// </summary>
/// <remarks>This validator ensures that the <c>Email</c> property of the <see cref="ChangeUserEmailRequest"/>  is
/// a valid email address and is not empty. If the validation fails, an appropriate error message is provided.</remarks>
public class ChangeUserEmailRequestValidator : AbstractValidator<ChangeUserEmailRequest>
{
    /// <inheritdoc/>
    public ChangeUserEmailRequestValidator() {
        RuleFor(x => x.Email).EmailAddress().NotEmpty().WithMessage("Email is required.");
    }
}
