using FluentValidation;
using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Server.Endpoints.Validators;

/// <summary>
/// Validator for case request
/// </summary>
public class AdminCaseTypesValidator : AbstractValidator<CaseTypeRequest>
{
    /// <inheritdoc/>
    public AdminCaseTypesValidator() {
        RuleFor(x => x.DataSchema).NotEmpty();
    }
}
