using FluentValidation;
using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Server.Endpoints.Validators;
/// <summary>
/// Validator for case request
/// </summary>
public class EditCaseRequestValidator : AbstractValidator<EditCaseRequest>
{
    /// <inheritdoc/>
    public EditCaseRequestValidator() {
        RuleFor(x => x.Data).NotNull();
    }
}

/// <summary>
/// Validator for action request
/// </summary>
public class ActionRequestValidator : AbstractValidator<ActionRequest>
{
    /// <inheritdoc/>
    public ActionRequestValidator() {
        RuleFor(x => x.Id).NotEmpty();
    }
}