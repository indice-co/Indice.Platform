using FluentValidation;
using Indice.Configuration;
using Indice.Features.Cases.Core.Models.Requests;

namespace Indice.Features.Cases.Server.Endpoints.Validators;

/// <summary>Validator for <see cref="UpdateCaseRelationKeyRequest"/>.</summary>
public class UpdateCaseRelationKeyRequestValidator : AbstractValidator<UpdateCaseRelationKeyRequest>
{
    /// <inheritdoc/>
    public UpdateCaseRelationKeyRequestValidator() {
        RuleFor(x => x.RelationKey).MaximumLength(CaseMetadataValidator.RelationKeyMaxLength);
    }
}

/// <summary>Validator for <see cref="AddRelatedCasesRequest"/>.</summary>
public class AddRelatedCasesRequestValidator : AbstractValidator<AddRelatedCasesRequest>
{
    /// <inheritdoc/>
    public AddRelatedCasesRequestValidator() {
        RuleFor(x => x.CaseIds).NotEmpty();
    }
}

/// <summary>Validator for <see cref="UpdateCaseGroupRequest"/>.</summary>
public class UpdateCaseGroupRequestValidator : AbstractValidator<UpdateCaseGroupRequest>
{
    /// <inheritdoc/>
    public UpdateCaseGroupRequestValidator() {
        RuleFor(x => x.GroupId).MaximumLength(TextSizePresets.M128);
    }
}
