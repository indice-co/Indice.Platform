﻿using Indice.Types;

namespace Indice.Features.Messages.Core.Models;

/// <summary>Models a campaign.</summary>
public class Campaign
{
    /// <summary>The unique identifier of the campaign.</summary>
    public Guid Id { get; set; }
    /// <summary>The title of the campaign.</summary>
    public string Title { get; set; }
    /// <summary>The content of the campaign.</summary>
    public MessageContentDictionary Content { get; set; } = [];
    /// <summary>Defines a (call-to-action) link.</summary>
    public Hyperlink ActionLink { get; set; }
    /// <summary>Determines if a campaign is published.</summary>
    public bool Published { get; set; }
    /// <summary>Specifies the time period that a campaign is active.</summary>
    public Period ActivePeriod { get; set; }
    /// <summary>Determines if campaign targets all user base.</summary>
    public bool IsGlobal { get; set; }
    /// <summary>The type details of the campaign.</summary>
    public MessageType Type { get; set; }
    /// <summary>The distribution list of the campaign.</summary>
    public DistributionList DistributionList { get; set; }
    /// <summary>The delivery channel of a campaign.</summary>
    public MessageChannelKind MessageChannelKind { get; set; }
    /// <summary>The base href to access the media.</summary>
    public string MediaBaseHref { get; set; }
    /// <summary>Optional data for the campaign.</summary>
    public dynamic Data { get; set; }
    /// <summary>Specifies the principal that created the campaign.</summary>
    public string CreatedBy { get; set; }
    /// <summary>Specifies when a campaign was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Specifies the principal that update the campaign.</summary>
    public string UpdatedBy { get; set; }
    /// <summary>Specifies when a campaign was updated.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
