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