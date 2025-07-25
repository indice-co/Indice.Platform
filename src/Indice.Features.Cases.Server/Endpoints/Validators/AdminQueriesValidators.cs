using FluentValidation;
using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Server.Endpoints.Validators;

/// <summary>
/// Validator for save query request
/// </summary>
public class SaveQueryRequestValidator : AbstractValidator<SaveQueryRequest>
{
    /// <inheritdoc/>
    public SaveQueryRequestValidator() {
        RuleFor(x => x.FriendlyName).NotEmpty();
        RuleFor(x => x.Parameters).NotEmpty();
    }
}