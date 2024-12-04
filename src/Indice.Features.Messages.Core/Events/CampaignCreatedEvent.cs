﻿using Indice.Features.Messages.Core.Models;
using Indice.Types;

namespace Indice.Features.Messages.Core.Events;

/// <summary>The event model used when a new campaign is published.</summary>
public class CampaignCreatedEvent
{
    /// <summary>The unique identifier of the campaign.</summary>
    public Guid Id { get; set; }
    /// <summary>The title of the campaign.</summary>
    public string Title { get; set; }
    /// <summary>The content of the campaign.</summary>
    public MessageContentDictionary Content { get; set; } = [];
    /// <summary>Determines if a campaign is published.</summary>
    public bool Published { get; set; }
    /// <summary>Specifies the time period that a campaign is active.</summary>
    public Period ActivePeriod { get; set; }
    /// <summary>Determines if campaign targets all user base.</summary>
    public bool IsGlobal { get; set; }
    /// <summary>Optional data for the campaign.</summary>
    public dynamic Data { get; set; }
    /// <summary>The delivery channel of a campaign.</summary>
    public MessageChannelKind MessageChannelKind { get; set; }
    /// <summary>The base href to access the media.</summary>
    public string MediaBaseHref { get; set; }
    /// <summary>The distribution list of the campaign.</summary>
    public Guid? DistributionListId { get; set; }
    /// <summary>Determines whether the distribution list already exists or is new.</summary>
    public bool IsNewDistributionList { get; set; }
    /// <summary>The type details of the campaign.</summary>
    public MessageType Type { get; set; }
    /// <summary>The call to action <see cref="Hyperlink"/> of the campaign.</summary>
    public Hyperlink ActionLink { get; set; }
    /// <summary>Defines a list of user identifiers that constitutes the audience of the campaign.</summary>
    public List<string> RecipientIds { get; set; } = [];
    /// <summary>List of anonymous contacts not available through any of the existing contact resolvers. Use this list if recipient id is not known/available or the message will be fire and forget.</summary>
    public List<ContactAnonymous> Recipients { get; set; } = [];

    /// <summary>Creates a <see cref="CampaignCreatedEvent"/> instance from a <see cref="Campaign"/> instance.</summary>
    /// <param name="campaign">Models a campaign.</param>
    /// <param name="recipientIds">Defines a list of user identifiers that constitutes the audience of the campaign.</param>
    /// <param name="recipients">Defines a list of additional anonymous contacts to be also audience of the campaign.</param>
    /// <param name="isNewDistributionList">Determines whether the distribution list already exists or is new.</param>
    public static CampaignCreatedEvent FromCampaign(Campaign campaign, List<string> recipientIds = null, List<ContactAnonymous> recipients = null, bool isNewDistributionList = true) => new() {
        ActivePeriod = campaign.ActivePeriod,
        Content = campaign.Content,
        Data = campaign.Data,
        MessageChannelKind = campaign.MessageChannelKind,
        DistributionListId = campaign.DistributionList?.Id,
        Id = campaign.Id,
        Title = campaign.Title,
        Type = campaign.Type,
        ActionLink = campaign.ActionLink,
        MediaBaseHref = campaign.MediaBaseHref,
        IsGlobal = campaign.IsGlobal,
        IsNewDistributionList = isNewDistributionList,
        Published = campaign.Published,
        RecipientIds = recipientIds ?? [],
        Recipients = recipients ?? []
    };
}
