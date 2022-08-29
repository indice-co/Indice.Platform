using FluentValidation;
using Indice.Features.Messages.Core.Models.Requests;

namespace Indice.Features.Messages.Core.Services.Validators
{
    /// <summary>Contains validation logic for <see cref="CreateDistributionListContactRequest"/>.</summary>
    public class CreateDistributionListContactRequestValidator : AbstractValidator<CreateDistributionListContactRequest>
    {
        /// <summary>Creates a new instance of <see cref="CreateDistributionListRequestValidator"/>.</summary>
        public CreateDistributionListContactRequestValidator() {
            RuleFor(x => x.FullName).NotEmpty();
        }
    }
}
