using FluentValidation;
using Indice.Features.Cases.Core.Models.Requests;

namespace Indice.Features.Cases.Server.Endpoints.Validators;

/// <summary>
/// Validator for the checkpoint type create/update request.
/// </summary>
/// <remarks>The uniqueness of <see cref="CheckpointTypeRequest.Code"/> per case type is checked by the checkpoint type service, since it requires database access.</remarks>
public class CheckpointTypeRequestValidator : AbstractValidator<CheckpointTypeRequest>
{
    /// <summary>The maximum length of the <see cref="CheckpointTypeRequest.Code"/>.</summary>
    public const int CodeMaxLength = 64;

    /// <inheritdoc/>
    public CheckpointTypeRequestValidator() {
        RuleFor(x => x.CaseTypeId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(CodeMaxLength);
        RuleFor(x => x.Status).IsInEnum();
    }
}
