using Indice.Features.Messages.Core.Models.Requests;

namespace Indice.Features.Messages.Core.Services.Validators
{
    /// <summary>Contains validation logic for <see cref="UpdateCampaignRequest"/>.</summary>
    public class UpdateCampaignRequestValidator : CampaignRequestValidator<UpdateCampaignRequest>
    {
        /// <summary>Creates a new instance of <see cref="UpdateCampaignRequestValidator"/>.</summary>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
        public UpdateCampaignRequestValidator(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }
}
