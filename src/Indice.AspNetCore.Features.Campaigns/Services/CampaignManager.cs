using System;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.AspNetCore.Features.Campaigns.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Features.Campaigns
{
    /// <summary>
    /// 
    /// </summary>
    public class CampaignManager
    {
        /// <summary>
        /// Creates a new instance of <see cref="CampaignManager"/>.
        /// </summary>
        public CampaignManager(IServiceProvider serviceProvider) {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            CampaignService = ServiceProvider.GetRequiredService<ICampaignService>();
        }

        private IServiceProvider ServiceProvider { get; }
        private ICampaignService CampaignService { get; }

        /// <summary>
        /// Creates a new campaign.
        /// </summary>
        /// <param name="campaign">The request model used to create a new campaign.</param>
        public async Task<CampaignResult> CreateCampaign(CreateCampaignRequest campaign) {
            var validator = ServiceProvider.GetRequiredService<CreateCampaignRequestValidator>();
            var validationResult = validator.Validate(campaign);
            if (!validationResult.IsValid) {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToArray();
                return CampaignResult.Fail(errorMessages);
            }
            var createdCampaign = await CampaignService.CreateCampaign(campaign);
            campaign.Id = createdCampaign.Id;
            return CampaignResult.Success();
        }

        /// <summary>
        /// Retrieves the campaign with the specified id.
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <returns>The campaign with the specified id, otherwise null.</returns>
        public Task<CampaignDetails> GetCampaignById(Guid campaignId) => CampaignService.GetCampaignById(campaignId);

        /// <summary>
        /// Creates a new campaign type.
        /// </summary>
        /// <param name="campaignType">The request model used to create a new campaign type.</param>
        public async Task<CampaignResult> CreateCampaignType(UpsertCampaignTypeRequest campaignType) {
            var validator = ServiceProvider.GetRequiredService<UpsertCampaignTypeRequestValidator>();
            var validationResult = validator.Validate(campaignType);
            if (!validationResult.IsValid) {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToArray();
                return CampaignResult.Fail(errorMessages);
            }
            var createdCampaignType = await CampaignService.CreateCampaignType(campaignType);
            campaignType.Id = createdCampaignType.Id;
            return CampaignResult.Success();
        }

        /// <summary>
        /// Retrieves the campaign type with the specified name.
        /// </summary>
        /// <param name="name">The name of the campaign type to look for.</param>
        public Task<CampaignType> GetCampaignTypeByName(string name) => CampaignService.GetCampaignTypeByName(name);
    }
}
