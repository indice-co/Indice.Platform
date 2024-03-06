using FluentValidation;
using Indice.Features.Risk.Core.Models;

namespace Indice.Features.Risk.Core.Validators;

/// <summary>
/// Base validator for validating <see cref="RuleOptions"/>
/// </summary>
/// <typeparam name="TOptions"></typeparam>
public class RuleOptionsValidator<TOptions> : AbstractValidator<TOptions> where TOptions : RuleOptions
{
    /// <summary>
    /// The validation rules.
    /// </summary>
    public RuleOptionsValidator() {
        RuleFor(x => x.FriendlyName)
            .NotEmpty()
            .NotNull();
    }
}