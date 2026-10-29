using FluentValidation;
using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Server.Endpoints.Validators;

    

/// <summary>
/// Validator for My Create Draft Case Request
/// </summary>
public class CreateDraftCaseRequestValidator : AbstractValidator<CreateDraftCaseRequest>
{
    /// <inheritdoc/>
    public CreateDraftCaseRequestValidator() {
        RuleFor(x => x.CaseTypeCode).NotEmpty();
        RuleFor(x=> x.ReferenceNumber).MaximumLength(128);
        RuleFor(x => x.Channel).MaximumLength(128);
        RuleFor(x => x.RelationKey).MaximumLength(CaseMetadataValidator.RelationKeyMaxLength);
        RuleFor(x => x.Metadata).SetValidator(x => new CaseMetadataValidator())
            .When(x => string.IsNullOrWhiteSpace(x.RelationKey));
    }
}


/// <summary>
/// Validator for My Update Case Request
/// </summary>
public class UpdateCaseRequestValidator : AbstractValidator<UpdateCaseRequest>
{
    /// <inheritdoc/>
    public UpdateCaseRequestValidator() {
        RuleFor(x => x.Data).NotNull();
    }
}