using Indice.Features.Messages.Core.Manager.Commands;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;

namespace Indice.Features.Messages.Core.Manager
{
    /// <summary>
    /// A manager class that helps work with the Campaigns API infrastructure.
    /// </summary>
    public class NotificationsManager
    {
        /// <summary>
        /// Creates a new instance of <see cref="NotificationsManager"/>.
        /// </summary>
        public NotificationsManager(
            ICampaignService campaignService,
            IMessageTypeService messageTypeService,
            IDistributionListService distributionListService,
            IContactService contactService,
            ITemplateService templateService,
            CreateCampaignRequestValidator createCampaignValidator,
            UpsertMessageTypeRequestValidator messageTypeValidator
        ) {
            CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
            MessageTypeService = messageTypeService ?? throw new ArgumentNullException(nameof(messageTypeService));
            DistributionListService = distributionListService ?? throw new ArgumentNullException(nameof(distributionListService));
            ContactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
            TemplateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            CreateCampaignValidator = createCampaignValidator ?? throw new ArgumentNullException(nameof(createCampaignValidator));
            MessageTypeValidator = messageTypeValidator ?? throw new ArgumentNullException(nameof(messageTypeValidator));
        }

        private ICampaignService CampaignService { get; }
        private IMessageTypeService MessageTypeService { get; }
        private IDistributionListService DistributionListService { get; }
        private IContactService ContactService { get; }
        private ITemplateService TemplateService { get; }
        private CreateCampaignRequestValidator CreateCampaignValidator { get; }
        private UpsertMessageTypeRequestValidator MessageTypeValidator { get; }

        /// <summary>
        /// Creates a new campaign.
        /// </summary>
        /// <param name="campaign">The request model used to create a new campaign.</param>
        public Task<CreateCampaignResult> CreateCampaign(CreateCampaignCommand campaign) {
            var request = Mapper.ToCreateCampaignRequest(campaign);
            return CreateCampaignInternal(request);
        }

        internal async Task<CreateCampaignResult> CreateCampaignInternal(CreateCampaignRequest request, bool? validateRules = true) {
            if (validateRules.Value) {
                // Apply validation rules to incoming data.
                var validationResult = CreateCampaignValidator.Validate(request);
                if (!validationResult.IsValid) {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).Distinct().ToArray();
                    return CreateCampaignResult.Fail(errorMessages);
                }
            }
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // If a distribution list id is not set, then we create a new list.
            if (!request.DistributionListId.HasValue) {
                var createdList = await DistributionListService.Create(new CreateDistributionListRequest {
                    Name = $"{request.Title} - {timestamp}"
                });
                request.DistributionListId = createdList.Id;
            }
            if (request.Content.Count > 0) {
                var createdTemplate = await TemplateService.Create(new CreateTemplateRequest {
                    Content = request.Content,
                    Name = $"{request.Title} - {timestamp}"
                });
                request.TemplateId = createdTemplate.Id;
            }
            // Create campaign in the store.
            var createdCampaign = await CampaignService.Create(request);
            // Create contacts as part of a bulk insert only using the recipient ids.
            if (request.RecipientIds.Any()) {
                var contacts = new List<CreateContactRequest>();
                contacts.AddRange(request.RecipientIds.Select(id => new CreateContactRequest {
                    RecipientId = id,
                    DistributionListId = request.DistributionListId
                }));
                await ContactService.CreateMany(contacts);
            }
            return CreateCampaignResult.Success(createdCampaign.Id);
        }

        /// <summary>
        /// Retrieves the campaign with the specified id.
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <returns>The campaign with the specified id, otherwise null.</returns>
        public Task<CampaignDetails> GetCampaignById(Guid campaignId) => CampaignService.GetById(campaignId);

        /// <summary>
        /// Creates a new campaign type.
        /// </summary>
        /// <param name="campaignType">The request model used to create a new campaign type.</param>
        public async Task<CampaignResult> CreateMessageType(UpsertMessageTypeRequest campaignType) {
            var validationResult = MessageTypeValidator.Validate(campaignType);
            if (!validationResult.IsValid) {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToArray();
                //return CampaignResult.Fail(errorMessages);
            }
            var createdCampaignType = await MessageTypeService.Create(campaignType);
            campaignType.Id = createdCampaignType.Id;
            //return CampaignResult.Success();
            return null;
        }

        /// <summary>
        /// Retrieves the campaign type with the specified name.
        /// </summary>
        /// <param name="name">The name of the campaign type to look for.</param>
        public Task<MessageType> GetMessageTypeByName(string name) => MessageTypeService.GetByName(name);
    }
}
