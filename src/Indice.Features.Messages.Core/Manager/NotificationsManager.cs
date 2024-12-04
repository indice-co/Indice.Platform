﻿using Indice.Extensions;
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
/// <remarks>Creates a new instance of <see cref="NotificationsManager"/>.</remarks>
public class NotificationsManager(
    ICampaignService campaignService,
    ICampaignAttachmentService campaignAttachmentService,
    IMessageTypeService messageTypeService,
    IDistributionListService distributionListService,
    ITemplateService templateService,
    CreateCampaignRequestValidator createCampaignValidator,
    CreateMessageTypeRequestValidator messageTypeValidator,
    IEventDispatcherFactory eventDispatcherFactory
    )
{
    private ICampaignService CampaignService { get; } = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
    private ICampaignAttachmentService CampaignAttachmentService { get; } = campaignAttachmentService ?? throw new ArgumentNullException(nameof(campaignAttachmentService));
    private IMessageTypeService MessageTypeService { get; } = messageTypeService ?? throw new ArgumentNullException(nameof(messageTypeService));
    private IDistributionListService DistributionListService { get; } = distributionListService ?? throw new ArgumentNullException(nameof(distributionListService));
    private ITemplateService TemplateService { get; } = templateService ?? throw new ArgumentNullException(nameof(templateService));
    private CreateCampaignRequestValidator CreateCampaignValidator { get; } = createCampaignValidator ?? throw new ArgumentNullException(nameof(createCampaignValidator));
    private CreateMessageTypeRequestValidator MessageTypeValidator { get; } = messageTypeValidator ?? throw new ArgumentNullException(nameof(messageTypeValidator));
    private IEventDispatcher EventDispatcher { get; } = eventDispatcherFactory.Create(KeyedServiceNames.EventDispatcherServiceKey) ?? throw new ArgumentNullException(nameof(eventDispatcherFactory));

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
    /// <param name="attachment">An attachement available to email and inbox channels</param>
    /// <param name="recipientIds">Defines a list of user identifiers that constitutes the audience of the campaign.</param>
    public async Task<CreateCampaignResult> SendMessageToRecipients(string title, Dictionary<MessageChannelKind, MessageContent> templates, Period period = null,
        Hyperlink actionLink = null, string type = null, dynamic data = null, FileAttachment attachment = null, params string[] recipientIds) {
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
            TypeId = typeId,
            Attachments = attachment is null ? [] : [attachment],
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
    /// <param name="attachment">An attachement available to email and inbox channels</param>
    public Task<CreateCampaignResult> SendMessageToRecipient(string recipientId, string title, Dictionary<MessageChannelKind, MessageContent> templates, Period period = null,
        Hyperlink actionLink = null, string type = null, dynamic data = null, FileAttachment attachment = null) =>
        SendMessageToRecipients(title, templates, period, actionLink, type, data, attachment, recipientId);

    /// <summary>Creates a new message for the specified recipient.</summary>
    /// <param name="recipientId">The id of the recipient.</param>
    /// <param name="title">The title of the campaign.</param>
    /// <param name="channels">The delivery channels of a campaign.</param>
    /// <param name="template">The content of the campaign. If handlebars are found, then data binding will occur between content and <paramref name="data"/>.</param>
    /// <param name="period">Specifies the time period that a campaign is active. If not set campaign inbox is shown indefinitely.</param>
    /// <param name="actionLink">Defines a (call-to-action) link.</param>
    /// <param name="type">The id or name of the campaign type.</param>
    /// <param name="data">Optional data for the campaign.</param>
    /// <param name="attachment">An attachement available to email and inbox channels</param>
    public Task<CreateCampaignResult> SendMessageToRecipient(string recipientId, string title, MessageChannelKind channels, MessageContent template, Period period = null,
        Hyperlink actionLink = null, string type = null, dynamic data = null, FileAttachment attachment = null) =>
        SendMessageToRecipients(title, channels.GetFlagValues().ToDictionary(x => x, y => template), period, actionLink, type, data, attachment, recipientId);

    /// <summary>Creates a new message for the specified recipient.</summary>
    /// <param name="recipientId">The id of the recipient.</param>
    /// <param name="title">The title of the campaign.</param>
    /// <param name="channels">The delivery channels of a campaign.</param>
    /// <param name="templateId">The content of the campaign. If handlebars are found, then data binding will occur between content and <paramref name="data"/>.</param>
    /// <param name="period">Specifies the time period that a campaign is active. If not set campaign inbox is shown indefinitely.</param>
    /// <param name="actionLink">Defines a (call-to-action) link.</param>
    /// <param name="type">The id or name of the campaign type.</param>
    /// <param name="data">Optional data for the campaign.</param>
    /// <param name="attachment">An attachement available to email and inbox channels</param>
    public Task<CreateCampaignResult> SendMessageToRecipient(string recipientId, string title, MessageChannelKind channels, Guid templateId, Period period = null,
        Hyperlink actionLink = null, string type = null, dynamic data = null, FileAttachment attachment = null) =>
        SendMessageToRecipients(title, channels, templateId, period, actionLink, type, data, attachment, recipientId);

    /// <summary>Creates a new message for the specified recipients.</summary>
    /// <param name="title">The title of the campaign.</param>
    /// <param name="channels">The delivery channels of a campaign.</param>
    /// <param name="templateId">The content of the campaign. If handlebars are found, then data binding will occur between content and <paramref name="data"/>.</param>
    /// <param name="period">Specifies the time period that a campaign is active. If not set campaign inbox is shown indefinitely.</param>
    /// <param name="actionLink">Defines a (call-to-action) link.</param>
    /// <param name="type">The id or name of the campaign type.</param>
    /// <param name="data">Optional data for the campaign.</param>
    /// <param name="attachment">An attachement available to email and inbox channels</param>
    /// <param name="recipientIds">Defines a list of user identifiers that constitutes the audience of the campaign.</param>
    public async Task<CreateCampaignResult> SendMessageToRecipients(string title, MessageChannelKind channels, Guid templateId, Period period = null,
        Hyperlink actionLink = null, string type = null, dynamic data = null, FileAttachment attachment = null, params string[] recipientIds) {
        var template = await TemplateService.GetById(templateId);
        var arrayOfChannel = channels.GetFlagValues();
        return await SendMessageToRecipients(title, template.Content
                                                            .Select(x => new KeyValuePair<MessageChannelKind, MessageContent>(Enum.Parse<MessageChannelKind>(x.Key, ignoreCase: true), x.Value))
                                                            .Where(x => arrayOfChannel.Contains(x.Key))
                                                            .ToDictionary(x => x.Key, x => x.Value), period, actionLink, type, data, attachment, recipientIds);
    }

    /// <summary>Creates a new message for the specified recipients.</summary>
    /// <param name="title">The title of the campaign.</param>
    /// <param name="channels">The delivery channels of a campaign.</param>
    /// <param name="template">The content of the campaign. If handlebars are found, then data binding will occur between content and <paramref name="data"/>.</param>
    /// <param name="period">Specifies the time period that a campaign is active. If not set campaign inbox is shown indefinitely.</param>
    /// <param name="actionLink">Defines a (call-to-action) link.</param>
    /// <param name="type">The id or name of the campaign type.</param>
    /// <param name="data">Optional data for the campaign.</param>
    /// <param name="attachment">An attachement available to email and inbox channels</param>
    /// <param name="recipientIds">Defines a list of user identifiers that constitutes the audience of the campaign.</param>
    public Task<CreateCampaignResult> SendMessageToRecipients(string title, MessageChannelKind channels, MessageContent template, Period period = null,
        Hyperlink actionLink = null, string type = null, dynamic data = null, FileAttachment attachment = null, params string[] recipientIds) =>
        SendMessageToRecipients(title, channels.GetFlagValues().ToDictionary(x => x, y => template), period, actionLink, type, data, attachment, recipientIds);

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
        // create the attachemtns
        if (request.Attachments.Any()) {
            if (request.Attachments.Count > 3) { 
                return CreateCampaignResult.Fail("Too many attachments. Maximum attachent size for Notification manager is '3'");
            }
            
            try { 
                var createAttachmentTasks = request.Attachments.Where(x => x is not null).Select(x => CampaignAttachmentService.Create(x));
                var attachments = await Task.WhenAll(createAttachmentTasks);
                request.AttachmentIds = attachments.Select(x => x.Id).ToList();
            } catch (Exception ex) {
                return CreateCampaignResult.Fail("Failed to store the attachments. Check storage or database settings", ex.Message);
            }
        }
        // use the teplate content if exists
        if (request.MessageTemplateId.HasValue && request.Content.Count == 0) {
            var template = await TemplateService.GetById(request.MessageTemplateId.Value);
            if (template == null) {
                return CreateCampaignResult.Fail($"The selected Template with Id:({request.MessageTemplateId}) does not exist");
            }
            request.Content = template.Content;
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
