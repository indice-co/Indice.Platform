using Indice.Types;

namespace Indice.Features.Messages.Core.Models;

/// <summary>Models a user message.</summary>
public class Message
{
    /// <summary>The unique identifier of the user message.</summary>
    public Guid Id { get; set; }
    /// <summary>The sender identity. This is optional and will default to the default settings.</summary>
    /// <remarks>When email it is the from account no-reply@domain.com. When SMS it is the sending name i.e. INDICE</remarks>
    public string Sender { get; set; }
    /// <summary>The title of the user message.</summary>
    public string Title { get; set; }
    /// <summary>The content of the user message.</summary>
    public string Content { get; set; }
    /// <summary>Determines if a message is read by the user.</summary>
    public bool IsRead { get; set; }
    /// <summary>Defines a (call-to-action) link.</summary>
    public Hyperlink ActionLink { get; set; }
    /// <summary>The URL to the attachment.</summary>
    public string AttachmentUrl { get; set; }
    /// <summary>Defines when the message was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Specifies the time period that a campaign is active.</summary>
    public Period ActivePeriod { get; set; }
    /// <summary>The type details of the campaign.</summary>
    public MessageType Type { get; set; }
    /// <summary>Indicates that the message is part of a global campaign and requires the appropriate substitutions to be personalized.</summary>
    internal bool RequiresSubstitutions { get; set; }
    /// <summary>The Data property of Campaign Metadata.</summary>
    internal dynamic CampaignData { get; set; }
}
