using Indice.Extensions;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Manager.Commands;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;
using Indice.Services;
using Indice.Types;

namespace Indice.Features.Messages.Core.Manager;

/// <summary>A manager class that helps work with the Campaigns API infrastructure.</summary>
public class NotificationsManager
{
    /// <summary>Creates a new instance of <see cref="NotificationsManager"/>.</summary>
    public NotificationsManager(
        ICampaignService campaignService,
        IMessageTypeService messageTypeService,
        IDistributionListService distributionListService,
        ITemplateService templateService,
        CreateCampaignRequestValidator createCampaignValidator,
        CreateMessageTypeRequestValidator messageTypeValidator,
        Func<string, IEventDispatcher> getEventDispatcher
    ) {
        CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
        MessageTypeService = messageTypeService ?? throw new ArgumentNullException(nameof(messageTypeService));
        DistributionListService = distributionListService ?? throw new ArgumentNullException(nameof(distributionListService));
        TemplateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        CreateCampaignValidator = createCampaignValidator ?? throw new ArgumentNullException(nameof(createCampaignValidator));
        MessageTypeValidator = messageTypeValidator ?? throw new ArgumentNullException(nameof(messageTypeValidator));
        EventDispatcher = getEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey) ?? throw new ArgumentNullException(nameof(getEventDispatcher));
    }

    private ICampaignService CampaignService { get; }
    private IMessageTypeService MessageTypeService { get; }
    private IDistributionListService DistributionListService { get; }
    private ITemplateService TemplateService { get; }
    private CreateCampaignRequestValidator CreateCampaignValidator { get; }
    private CreateMessageTypeRequestValidator MessageTypeValidator { get; }
    private IEventDispatcher EventDispatcher { get; }

    /// <summary>Creates a new campaign.</summary>
    /// <param name="campaign">The request model used to create a new campaign.</param>
    public Task<CreateCampaignResult> CreateCampaign(CreateCampaignCommand campaign) {
        var request = Mapper.ToCreateCampaignRequest(campaign);
        return CreateCampaignInternal(request);
    }

    /// <summary>Creates a new message for the specified recipients.</summary>
    /// <param name="title">The title of the campaign.</param>
    /// <param name="templates">The content of the campaign. If handlebars are found, then data binding will occur between content and <paramref name="data"/>.</param>
    /// <param name="period">Specifies the time period that a campaign is active. If not set campaign inbox is shown indefinitely.</param>
    /// <param name="actionLink">Defines a (call-to-action) link.</param>
    /// <param name="type">The id or name of the campaign type.</param>
    /// <param name="data">Optional data for the campaign.</param>
    /// <param name="recipientIds">Defines a list of user identifiers that constitutes the audience of the campaign.</param>
    public async Task<CreateCampaignResult> SendMessageToRecipients(string title, Dictionary<MessageChannelKind, MessageContent> templates, Period period = null,
        Hyperlink actionLink = null, string type = null, dynamic data = null, params string[] recipientIds) {
        Guid? typeId = null;
        if (!string.IsNullOrWhiteSpace(type)) {
            var isGuid = Guid.TryParse(type, out var typeGuid);
            if (isGuid) {
                typeId = typeGuid;
            } else {
                var messageType = await GetMessageTypeByName(type);
                if (messageType is null) {
                    return CreateCampaignResult.Fail("Specified type id is not valid.");
                }
                typeId = messageType.Id;
            }
        }
        var request = new CreateCampaignRequest {
            ActionLink = actionLink,
            ActivePeriod = period,
            Content = new MessageContentDictionary(templates),
            Data = Mapper.ToExpandoObject(data),
            IsGlobal = false,
            Published = true,
            RecipientIds = recipientIds?.ToList(),
            Title = title,
            TypeId = typeId
        };
        return await CreateCampaignInternal(request);
    }

    /// <summary>Creates a new message for the specified recipient.</summary>
    /// <param name="recipientId">The id of the recipient.</param>
    /// <param name="title">The title of the campaign.</param>
    /// <param name="templates">The content of the campaign. If handlebars are found, then data binding will occur between content and <paramref name="data"/>.</param>
    /// <param name="period">Specifies the time period that a campaign is active. If not set campaign inbox is shown indefinitely.</param>
    /// <param name="actionLink">Defines a (call-to-action) link.</param>
    /// <param name="type">The id or name of the campaign type.</param>
    /// <param name="data">Optional data for the campaign.</param>
    public Task<CreateCampaignResult> SendMessageToRecipient(string recipientId, string title, Dictionary<MessageChannelKind, MessageContent> templates, Period period = null,
        Hyperlink actionLink = null, string type = null, dynamic data = null) =>
        SendMessageToRecipients(title, templates, period, actionLink, type, data, recipientId);

    /// <summary>Creates a new message for the specified recipient.</summary>
    /// <param name="recipientId">The id of the recipient.</param>
    /// <param name="title">The title of the campaign.</param>
    /// <param name="channels">The delivery channels of a campaign.</param>
    /// <param name="template">The content of the campaign. If handlebars are found, then data binding will occur between content and <paramref name="data"/>.</param>
    /// <param name="period">Specifies the time period that a campaign is active. If not set campaign inbox is shown indefinitely.</param>
    /// <param name="actionLink">Defines a (call-to-action) link.</param>
    /// <param name="type">The id or name of the campaign type.</param>
    /// <param name="data">Optional data for the campaign.</param>
    public Task<CreateCampaignResult> SendMessageToRecipient(string recipientId, string title, MessageChannelKind channels, MessageContent template, Period period = null,
        Hyperlink actionLink = null, string type = null, dynamic data = null) =>
        SendMessageToRecipients(title, channels.GetFlagValues().ToDictionary(x => x, y => template), period, actionLink, type, data, recipientId);

    /// <summary>Creates a new message for the specified recipient.</summary>
    /// <param name="recipientId">The id of the recipient.</param>
    /// <param name="title">The title of the campaign.</param>
    /// <param name="channels">The delivery channels of a campaign.</param>
    /// <param name="templateId">The content of the campaign. If handlebars are found, then data binding will occur between content and <paramref name="data"/>.</param>
    /// <param name="period">Specifies the time period that a campaign is active. If not set campaign inbox is shown indefinitely.</param>
    /// <param name="actionLink">Defines a (call-to-action) link.</param>
    /// <param name="type">The id or name of the campaign type.</param>
    /// <param name="data">Optional data for the campaign.</param>
    public Task<CreateCampaignResult> SendMessageToRecipient(string recipientId, string title, MessageChannelKind channels, Guid templateId, Period period = null,
        Hyperlink actionLink = null, string type = null, dynamic data = null) =>
        SendMessageToRecipients(title, channels, templateId, period, actionLink, type, data, recipientId);

    /// <summary>Creates a new message for the specified recipients.</summary>
    /// <param name="title">The title of the campaign.</param>
    /// <param name="channels">The delivery channels of a campaign.</param>
    /// <param name="templateId">The content of the campaign. If handlebars are found, then data binding will occur between content and <paramref name="data"/>.</param>
    /// <param name="period">Specifies the time period that a campaign is active. If not set campaign inbox is shown indefinitely.</param>
    /// <param name="actionLink">Defines a (call-to-action) link.</param>
    /// <param name="type">The id or name of the campaign type.</param>
    /// <param name="data">Optional data for the campaign.</param>
    /// <param name="recipientIds">Defines a list of user identifiers that constitutes the audience of the campaign.</param>
    public async Task<CreateCampaignResult> SendMessageToRecipients(string title, MessageChannelKind channels, Guid templateId, Period period = null,
        Hyperlink actionLink = null, string type = null, dynamic data = null, params string[] recipientIds) {
        var template = await TemplateService.GetById(templateId);
        return await SendMessageToRecipients(title, template.Content.ToDictionary(x => Enum.Parse<MessageChannelKind>(x.Key, ignoreCase: true), x => x.Value), period, actionLink, type, data, recipientIds);
    }

    /// <summary>Creates a new message for the specified recipients.</summary>
    /// <param name="title">The title of the campaign.</param>
    /// <param name="channels">The delivery channels of a campaign.</param>
    /// <param name="template">The content of the campaign. If handlebars are found, then data binding will occur between content and <paramref name="data"/>.</param>
    /// <param name="period">Specifies the time period that a campaign is active. If not set campaign inbox is shown indefinitely.</param>
    /// <param name="actionLink">Defines a (call-to-action) link.</param>
    /// <param name="type">The id or name of the campaign type.</param>
    /// <param name="data">Optional data for the campaign.</param>
    /// <param name="recipientIds">Defines a list of user identifiers that constitutes the audience of the campaign.</param>
    public Task<CreateCampaignResult> SendMessageToRecipients(string title, MessageChannelKind channels, MessageContent template, Period period = null,
        Hyperlink actionLink = null, string type = null, dynamic data = null, params string[] recipientIds) =>
        SendMessageToRecipients(title, channels.GetFlagValues().ToDictionary(x => x, y => template), period, actionLink, type, data, recipientIds);

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
        var isNewDistributionList = false;
        // If a distribution list id is not set, then we create a new list.
        if (!request.RecipientListId.HasValue && !request.IsGlobal) {
            var createdList = await DistributionListService.Create(new CreateDistributionListRequest {
                Name = $"{request.Title} - {timestamp}",
                IsSystemGenerated = true
            }, request.GetIncludedContacts());
            request.RecipientListId = createdList.Id;
            isNewDistributionList = true;
        }
        // Create campaign in the store.
        var createdCampaign = await CampaignService.Create(request);
        // Dispatch event that the campaign was created.
        await EventDispatcher.RaiseEventAsync(
            payload: CampaignCreatedEvent.FromCampaign(createdCampaign, request.RecipientIds, request.Recipients, isNewDistributionList),
            configure: builder => 
                builder.WrapInEnvelope()
                       .At(request.ActivePeriod?.From?.DateTime ?? DateTime.UtcNow)
                       .WithQueueName(EventNames.CampaignCreated)
        );
        return CreateCampaignResult.Success(createdCampaign);
    }

    /// <summary>Retrieves the campaign with the specified id.</summary>
    /// <param name="campaignId">The id of the campaign.</param>
    /// <returns>The campaign with the specified id, otherwise null.</returns>
    public Task<CampaignDetails> GetCampaignById(Guid campaignId) => CampaignService.GetById(campaignId);

    /// <summary>Creates a new campaign type.</summary>
    /// <param name="name">The name used to create a new campaign type.</param>
    public async Task<CreateMessageTypeResult> CreateMessageType(string name) {
        var campaignType = new CreateMessageTypeRequest { Name = name };
        var validationResult = MessageTypeValidator.Validate(campaignType);
        if (!validationResult.IsValid) {
            var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToArray();
            return CreateMessageTypeResult.Fail(errorMessages);
        }
        var createdCampaignType = await MessageTypeService.Create(campaignType);
        return CreateMessageTypeResult.Success(createdCampaignType.Id);
    }

    /// <summary>Retrieves the campaign type with the specified name.</summary>
    /// <param name="name">The name of the campaign type to look for.</param>
    public Task<MessageType> GetMessageTypeByName(string name) => MessageTypeService.GetByName(name);

    /// <summary>Gets a list of all available templates.</summary>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    /// <returns></returns>
    public Task<ResultSet<TemplateListItem>> GetTemplates(ListOptions options) => TemplateService.GetList(options);
}
