using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;

namespace Indice.Features.Messages.Core.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class MessageManager
    {
        /// <summary>
        /// Creates a new instance of <see cref="MessageManager"/>.
        /// </summary>
        public MessageManager(
            ICampaignService campaignService,
            IMessageTypeService messageTypeService,
            CreateCampaignRequestValidator createCampaignValidator,
            UpsertMessageTypeRequestValidator messageTypeValidator
        ) {
            CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
            MessageTypeService = messageTypeService ?? throw new ArgumentNullException(nameof(messageTypeService));
            CreateCampaignValidator = createCampaignValidator ?? throw new ArgumentNullException(nameof(createCampaignValidator));
            MessageTypeValidator = messageTypeValidator ?? throw new ArgumentNullException(nameof(messageTypeValidator));
        }

        private ICampaignService CampaignService { get; }
        private IMessageTypeService MessageTypeService { get; }
        private CreateCampaignRequestValidator CreateCampaignValidator { get; }
        private UpsertMessageTypeRequestValidator MessageTypeValidator { get; }

        /// <summary>
        /// Creates a new campaign.
        /// </summary>
        /// <param name="campaign">The request model used to create a new campaign.</param>
        public async Task<CampaignResult> Create(CreateCampaignRequest campaign) {
            var validationResult = CreateCampaignValidator.Validate(campaign);
            if (!validationResult.IsValid) {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToArray();
                return CampaignResult.Fail(errorMessages);
            }
            var createdCampaign = await CampaignService.Create(campaign);
            campaign.Id = createdCampaign.Id;
            return CampaignResult.Success();
        }

        /// <summary>
        /// Retrieves the campaign with the specified id.
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <returns>The campaign with the specified id, otherwise null.</returns>
        public Task<CampaignDetails> GetById(Guid campaignId) => CampaignService.GetById(campaignId);

        /// <summary>
        /// Creates a new campaign type.
        /// </summary>
        /// <param name="campaignType">The request model used to create a new campaign type.</param>
        public async Task<CampaignResult> CreateType(UpsertMessageTypeRequest campaignType) {
            var validationResult = MessageTypeValidator.Validate(campaignType);
            if (!validationResult.IsValid) {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToArray();
                return CampaignResult.Fail(errorMessages);
            }
            var createdCampaignType = await MessageTypeService.Create(campaignType);
            campaignType.Id = createdCampaignType.Id;
            return CampaignResult.Success();
        }

        /// <summary>
        /// Retrieves the campaign type with the specified name.
        /// </summary>
        /// <param name="name">The name of the campaign type to look for.</param>
        public Task<MessageType> GetTypeByName(string name) => MessageTypeService.GetByName(name);
    }
}
