using Indice.Types;

namespace Indice.Features.Messages.Core.Models;
/// <summary>Models a contact preference for a recipient.</summary>
public class ContactPreference
{
    /// <summary>Contact's locale.</summary>
    public string? Locale { get; set; }
    /// <summary>Indicates if user accepted conset to receive email.</summary>
    public bool ConsentCommercial { get; set; }
    /// <summary>Indicates if user accepted conset to receive email.</summary>
    public DateTimeOffset? ConsentCommercialDate { get; set; }
    /// <summary>Communication preferences per message type.</summary>
    public List<ContactCommunicationOption> Communication { get; set; } = [];
    /// <summary>Default communication preferences</summary>
    public List<ContactChannelOption> DefaultChannels { get; set; } = ContactChannelOption.FromKindFlags(ContactChannelKind.Any);
}

/// <summary>Models a contact preference for a recipient.</summary>
public class ContactCommunicationOption
{
    /// <summary>The name of a campaign type.</summary>
    public string? MessageTypeDisplayName { get; set; }
    /// <summary>The alias of a campaign type.</summary>
    public GuidOrAlias MessageTypeAlias { get; set; }
    /// <summary>The preferred delivery channels to receive messages.</summary>
    public List<ContactChannelOption> Channels { get; set; } = ContactChannelOption.FromKindFlags(ContactChannelKind.Any);
}

/// <summary>
/// Represents an option for configuring a contact channel.
/// </summary>
/// <remarks>This class is used to specify the type of contact channel and whether additional data should be
/// included when performing operations related to contact channels. The <see cref="Kind"/> property defines the type of
/// contact channel, and the <see cref="Include"/> property indicates whether additional data should be
/// included.</remarks>
public class ContactChannelOption
{
    /// <summary>Gets or sets the type of contact channel represented by this instance.</summary>
    public ContactChannelKind Kind { get; set; }
    /// <summary>Gets or sets a value indicating whether channel kind can be used or not. Defaults to true.</summary>
    public bool Include { get; set; } = true;


    /// <summary>
    /// Returns a list of <see cref="ContactChannelOption"/> based on the communication preferences. 
    /// </summary>
    /// <returns>The list</returns>
    public static List<ContactChannelOption> FromKindFlags(ContactChannelKind kind) => [
            new () { Kind = ContactChannelKind.Email, Include = kind.HasFlag(ContactChannelKind.Email) || kind == ContactChannelKind.Any },
            new () { Kind = ContactChannelKind.SMS, Include = kind.HasFlag(ContactChannelKind.SMS) || kind == ContactChannelKind.Any },
            new () { Kind = ContactChannelKind.PushNotification, Include = kind.HasFlag(ContactChannelKind.PushNotification) || kind == ContactChannelKind.Any }
        ];

    /// <summary>
    /// Converts a collection of <see cref="ContactChannelOption"/> to a <see cref="MessageChannelKind"/>.
    /// </summary>
    /// <param name="options">The list ti convert</param>
    /// <returns>A flags enum of <see cref="MessageChannelKind"/></returns>
    public static ContactChannelKind ToContactChannelKind(IEnumerable<ContactChannelOption> options) =>
        options.Where(x => x.Include)
               .Select(x => x.Kind)
               .ToFlags();

    /// <summary>
    /// Converts a collection of <see cref="ContactChannelOption"/> to a <see cref="MessageChannelKind"/>.
    /// </summary>
    /// <param name="options">The list ti convert</param>
    /// <param name="defaultOption">the default option to include always. Defaults to none but if passed will keep that option regardless of preferences.</param>
    /// <returns>A flags enum of <see cref="MessageChannelKind"/></returns>
    public static MessageChannelKind ToMessageChannelKind(IEnumerable<ContactChannelOption> options, MessageChannelKind defaultOption = MessageChannelKind.None) =>
        options.Where(x => x.Include)
               .Select(x => x.Kind)
               .Aggregate(defaultOption, (messageKind, contactKind) =>
                    messageKind = contactKind switch {
                        ContactChannelKind.Email => messageKind | MessageChannelKind.Email,
                        ContactChannelKind.SMS => messageKind | MessageChannelKind.SMS,
                        ContactChannelKind.PushNotification => messageKind | MessageChannelKind.PushNotification,
                        _ => messageKind
                    });
}