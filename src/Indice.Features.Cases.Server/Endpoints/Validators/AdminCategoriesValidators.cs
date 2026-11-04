using FluentValidation;
using Indice.Configuration;
using Indice.Features.Cases.Core.Models.Requests;

namespace Indice.Features.Cases.Server.Endpoints.Validators;

/// <summary>
/// Validator for the category create/update request.
/// </summary>
public class CategoryRequestValidator : AbstractValidator<CategoryRequest>
{
    /// <summary>The maximum length of the <see cref="CategoryRequest.Name"/>.</summary>
    public const int NameMaxLength = TextSizePresets.M128;
    /// <summary>The maximum length of the <see cref="CategoryRequest.Description"/>.</summary>
    public const int DescriptionMaxLength = TextSizePresets.M512;

    /// <inheritdoc/>
    public CategoryRequestValidator() {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(DescriptionMaxLength);
    }
}
