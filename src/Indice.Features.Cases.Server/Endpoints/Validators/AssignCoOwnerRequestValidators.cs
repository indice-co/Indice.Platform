using FluentValidation;
using Indice.Configuration;
using Indice.Features.Cases.Core.Models.Requests;

namespace Indice.Features.Cases.Server.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="AssignCoOwnerRequest"/>.
/// </summary>
public class AssignCoOwnerRequestValidators : AbstractValidator<AssignCoOwnerRequest>
{

    /// <inheritdoc/>
    public AssignCoOwnerRequestValidators() {
        RuleFor(x => x.CoOwner).NotNull();
        // The lengths follow the columns of the co-owner table.
        When(x => x.CoOwner is not null, () => {
            RuleFor(x => x.CoOwner.UserId).NotEmpty().MaximumLength(TextSizePresets.S64);
            RuleFor(x => x.CoOwner.Reference).MaximumLength(TextSizePresets.M128);
            RuleFor(x => x.CoOwner.FirstName).MaximumLength(TextSizePresets.M128);
            RuleFor(x => x.CoOwner.LastName).MaximumLength(TextSizePresets.M128);
            RuleFor(x => x.CoOwner.Tin).MaximumLength(TextSizePresets.S32);
            RuleFor(x => x.CoOwner.Permission).IsInEnum();
        });
    }
}

/// <summary>Validator for <see cref="UpdateCaseOwnerPermissionRequest"/>.</summary>
public class UpdateCaseOwnerPermissionRequestValidator : AbstractValidator<UpdateCaseOwnerPermissionRequest>
{
    /// <inheritdoc/>
    public UpdateCaseOwnerPermissionRequestValidator() {
        RuleFor(x => x.Permission).IsInEnum();
    }
}
