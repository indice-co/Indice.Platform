using FluentValidation;
using Indice.Configuration;
using Indice.Features.Risk.Server.Models;

namespace Indice.Features.Risk.Server.Validators;

internal class GetRiskRequestValidator : AbstractValidator<RiskModel>
{
    public GetRiskRequestValidator() {
        RuleFor(x => x.IpAddress).MaximumLength(TextSizePresets.M128);
        RuleFor(x => x.Name).MaximumLength(TextSizePresets.M256);
        RuleFor(x => x.SubjectId).NotEmpty().MaximumLength(TextSizePresets.M256);
        RuleFor(x => x.Type).NotEmpty().MaximumLength(TextSizePresets.M256);
    }
}
