using Indice.Features.Messages.Core.Manager.Commands;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;
using Indice.Types;

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
            ITemplateService templateService,
            CreateCampaignRequestValidator createCampaignValidator,
            UpsertMessageTypeRequestValidator messageTypeValidator
        ) {
            CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
            MessageTypeService = messageTypeService ?? throw new ArgumentNullException(nameof(messageTypeService));
            DistributionListService = distributionListService ?? throw new ArgumentNullException(nameof(distributionListService));
            TemplateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            CreateCampaignValidator = createCampaignValidator ?? throw new ArgumentNullException(nameof(createCampaignValidator));
            MessageTypeValidator = messageTypeValidator ?? throw new ArgumentNullException(nameof(messageTypeValidator));
        }

        private ICampaignService CampaignService { get; }
        private IMessageTypeService MessageTypeService { get; }
        private IDistributionListService DistributionListService { get; }
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

        /// <summary>
        /// Creates a new campaign for the specified recipients.
        /// </summary>
        /// <param name="title">The title of the campaign.</param>
        /// <param name="content">The content of the campaign.</param>
        /// <param name="messageChannelKind">The delivery channel of a campaign.</param>
        /// <param name="period">Specifies the time period that a campaign is active.</param>
        /// <param name="actionLink">Defines a (call-to-action) link.</param>
        /// <param name="typeId">The id of the campaign type.</param>
        /// <param name="published">Determines if a campaign is published.</param>
        /// <param name="data">Optional data for the campaign.</param>
        /// <param name="recipientIds">Defines a list of user identifiers that constitutes the audience of the campaign.</param>
        public Task<CreateCampaignResult> CreateCampaignForRecipients(string title, Dictionary<MessageChannelKind, MessageContent> content, MessageChannelKind messageChannelKind, Period period = null,
            Hyperlink actionLink = null, Guid? typeId = null, bool published = true, dynamic data = null, params string[] recipientIds) {
            var request = new CreateCampaignRequest {
                ActionLink = actionLink,
                ActivePeriod = period,
                Content = content.ToDictionary(x => x.Key.ToString(), y => y.Value),
                Data = data,
                IsGlobal = false,
                MessageChannelKind = messageChannelKind,
                Published = published,
                RecipientIds = recipientIds?.ToList(),
                Title = title,
                TypeId = typeId
            };
            return CreateCampaignInternal(request);
        }

        /// <summary>
        /// Creates a new campaign for the specified recipient.
        /// </summary>
        /// <param name="recipientId">The id of the recipient.</param>
        /// <param name="title">The title of the campaign.</param>
        /// <param name="content">The content of the campaign.</param>
        /// <param name="messageChannelKind">The delivery channel of a campaign.</param>
        /// <param name="period">Specifies the time period that a campaign is active.</param>
        /// <param name="actionLink">Defines a (call-to-action) link.</param>
        /// <param name="typeId">The id of the campaign type.</param>
        /// <param name="published">Determines if a campaign is published.</param>
        /// <param name="data">Optional data for the campaign.</param>
        public Task<CreateCampaignResult> CreateCampaignForRecipient(string recipientId, string title, Dictionary<MessageChannelKind, MessageContent> content, MessageChannelKind messageChannelKind, Period period = null,
            Hyperlink actionLink = null, Guid? typeId = null, bool published = true, dynamic data = null) =>
            CreateCampaignForRecipients(title, content, messageChannelKind, period, actionLink, typeId, published, data, recipientId);

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
                    CreatedBy = CreatedBy.Worker,
                    Name = $"{request.Title} - {timestamp}"
                });
                request.DistributionListId = createdList.Id;
            }
            if (request.TemplateId.HasValue) {
                var template = await TemplateService.GetById(request.TemplateId.Value);
                request.Content = template.Content;
            }
            // Create campaign in the store.
            var createdCampaign = await CampaignService.Create(request);
            return CreateCampaignResult.Success(createdCampaign);
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
        public async Task<CreateMessageTypeResult> CreateMessageType(UpsertMessageTypeRequest campaignType) {
            var validationResult = MessageTypeValidator.Validate(campaignType);
            if (!validationResult.IsValid) {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToArray();
                return CreateMessageTypeResult.Fail(errorMessages);
            }
            var createdCampaignType = await MessageTypeService.Create(campaignType);
            return CreateMessageTypeResult.Success(createdCampaignType.Id);
        }

        /// <summary>
        /// Retrieves the campaign type with the specified name.
        /// </summary>
        /// <param name="name">The name of the campaign type to look for.</param>
        public Task<MessageType> GetMessageTypeByName(string name) => MessageTypeService.GetByName(name);
    }
}
