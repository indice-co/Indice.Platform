using FluentValidation;
using Indice.Configuration;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Risk.Server.Validators;

internal class GetRiskRequestValidator<TRiskEvent, TRiskRequest> : AbstractValidator<TRiskRequest> 
    where TRiskEvent : DbRiskEvent, new()
    where TRiskRequest : RiskRequestBase<TRiskEvent>
{
    public GetRiskRequestValidator() {
        RuleFor(x => x.IpAddress).MaximumLength(TextSizePresets.M128);
        RuleFor(x => x.Name).MaximumLength(TextSizePresets.M256);
        RuleFor(x => x.SubjectId).NotEmpty().MaximumLength(TextSizePresets.M256);
        RuleFor(x => x.Type).NotEmpty().MaximumLength(TextSizePresets.M256);
    }
}
