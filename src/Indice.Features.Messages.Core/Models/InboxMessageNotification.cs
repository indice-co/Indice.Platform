namespace Indice.Features.Messages.Core.Models;

/// <summary>
/// The payload pushed to inbox subscribers (i.e. through SignalR) whenever a new inbox message becomes available.
/// </summary>
/// <remarks>Intentionally lightweight. Clients are expected to use the inbox api (<c>my/messages</c>) in order to retrieve the full message.</remarks>
public class InboxMessageNotification
{
    /// <summary>The unique identifier of the message as exposed by the inbox api (<c>my/messages/{messageId}</c>).</summary>
    public Guid Id { get; set; }
    /// <summary>The (personalized) inbox title of the message.</summary>
    public string? Title { get; set; }
    /// <summary>The type details of the message.</summary>
    public MessageType? Type { get; set; }
    /// <summary>Specifies the time period that the message is active.</summary>
    public Indice.Types.Period? ActivePeriod { get; set; }
    /// <summary>Indicates that the message is part of a global campaign (broadcast to all users).</summary>
    public bool IsGlobal { get; set; }
    /// <summary>Defines when the notification was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Creates an <see cref="InboxMessageNotification"/> from a <see cref="Events.CampaignCreatedEvent"/>.</summary>
    /// <param name="campaign">The campaign.</param>
    public static InboxMessageNotification FromCampaign(Events.CampaignCreatedEvent campaign) => new() {
        Id = campaign.Id,
        Title = campaign.Content.TryGetValue(nameof(MessageChannelKind.Inbox), out var content) ? content.Title : campaign.Title,
        Type = campaign.Type,
        ActivePeriod = campaign.ActivePeriod,
        IsGlobal = campaign.IsGlobal
    };

    /// <summary>Determines whether a campaign is currently within its active period (if any).</summary>
    /// <param name="activePeriod">The active period.</param>
    /// <param name="now">The current point in time.</param>
    internal static bool IsActive(Indice.Types.Period? activePeriod, DateTimeOffset now) =>
        (activePeriod?.From is null || activePeriod.From <= now) &&
        (activePeriod?.To is null || activePeriod.To >= now);
}
