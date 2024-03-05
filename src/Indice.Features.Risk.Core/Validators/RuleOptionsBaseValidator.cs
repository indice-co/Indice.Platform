using FluentValidation;
using Indice.Features.Risk.Core.Models;

namespace Indice.Features.Risk.Core.Validators;

/// <summary>
/// Base validator for validating <see cref="RuleOptionsBase"/>
/// </summary>
/// <typeparam name="TOptions"></typeparam>
public abstract class RuleOptionsBaseValidator<TOptions> : AbstractValidator<TOptions> where TOptions : RuleOptionsBase
{
    /// <summary>
    /// The validation rules.
    /// </summary>
    public RuleOptionsBaseValidator() {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
    }
}