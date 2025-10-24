namespace Indice.Features.Messages.Core.Models;

/// <summary>Models a contact in the system as a member of a distribution list.</summary>
public class Contact
{
    /// <summary>The unique id of the contact.</summary>
    public Guid? Id { get; internal set; }
    /// <summary>The recipient correlation code.</summary>
    public string? RecipientId { get; set; }
    /// <summary>Contact salutation (Mr, Mrs etc).</summary>
    public string? Salutation { get; set; }
    /// <summary>The first name.</summary>
    public string? FirstName { get; set; }
    /// <summary>The last name.</summary>
    public string? LastName { get; set; }
    /// <summary>The full name.</summary>
    public string? FullName { get; set; }
    /// <summary>The email.</summary>
    public string? Email { get; set; }
    /// <summary>The phone number.</summary>
    public string? PhoneNumber { get; set; }
    /// <summary>Specifies if user has unsubscribed.</summary>
    public bool? Unsubscribed { get; set; }
    /// <summary>Indicates when contact info were last updated.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
    /// <summary>Determines if there is a <see cref="RecipientId"/> involved.</summary>
    public bool IsAnonymous => string.IsNullOrWhiteSpace(RecipientId);
    /// <summary>Determines if there is an <see cref="Email"/> assigned to this contact.</summary>
    internal bool HasEmail => !string.IsNullOrWhiteSpace(Email);
    /// <summary>Determines if there is a <see cref="PhoneNumber"/> assigned to this contact.</summary>
    internal bool HasPhoneNumber => !string.IsNullOrWhiteSpace(PhoneNumber);
    /// <summary>Check if the contact has email or phone.</summary>
    internal bool IsEmpty => !HasEmail && !HasPhoneNumber;
    /// <summary>Resolved using a contact resolver.</summary>
    public bool Resolved { get; set; }
    /// <summary>Indicates the last time the contact was resolved using the contact resolver service.</summary>
    public DateTimeOffset? LastResolutionDate { get; set; }
    /// <summary>Communication Preferences </summary>
    public ContactPreference Preference { get; set; } = new ContactPreference();

    /// <summary>
    /// Determines the available communication channels for a contact based on their preferences, supported channels,
    /// and provided contact information.
    /// </summary>
    /// <remarks>This method evaluates the contact's communication preferences and available contact
    /// information to determine  which channels can be used. Channels are excluded if: <list type="bullet"> <item>The
    /// contact has explicitly excluded them in their preferences.</item> <item>The contact lacks the necessary
    /// information for the channel (e.g., no email address for email).</item> <item>The contact is anonymous, which
    /// excludes certain channels like push notifications.</item> </list></remarks>
    /// <param name="campaignChannels">The set of communication channels supported by the campaign.</param>
    /// <param name="campaignType">The message type </param>
    /// <param name="ignoreUserPreferences">Used to ignore the user preferences and use all available channels based on contact info.</param>
    /// <returns>A <see cref="MessageChannelKind"/> value representing the communication channels that are available for the
    /// contact. The result is determined by filtering the campaign-supported channels based on the contact's
    /// preferences,  available contact information, and anonymity status.</returns>
    public MessageChannelKind GetAvailableChannels(MessageChannelKind campaignChannels, MessageType? campaignType, bool ignoreUserPreferences) {
        // start with all channels that the campaign supports
        var availableChannels = campaignChannels;
        // remove channels that the contact does not prefer if we are not ignoring user preferences
        if (!ignoreUserPreferences) {
            var typeCommunicationPreference = Preference?.Communication?.FirstOrDefault(x => x.MessageType.Id == campaignType?.Id);
            if (typeCommunicationPreference != null) {
                var userSelectedChannels = ContactChannelOption.ToMessageChannelKind(typeCommunicationPreference.Channels);
                availableChannels &= userSelectedChannels;
            } else if (Preference?.DefaultChannels != null) {
                var userSelectedChannels = ContactChannelOption.ToMessageChannelKind(Preference.DefaultChannels);
                availableChannels &= userSelectedChannels;
            }
        }
        // remove channels that the contact does not support due to missing info
        if (!HasEmail) {
            availableChannels &= ~MessageChannelKind.Email;
        }
        if (!HasPhoneNumber) {
            availableChannels &= ~MessageChannelKind.SMS;
        }
        if (IsAnonymous) {
            availableChannels &= ~MessageChannelKind.PushNotification;
        }
        return availableChannels;
    }

    public string GetReceiverByChannel(MessageChannelKind channel) => channel switch {
        MessageChannelKind.Email => Email ?? "",
        MessageChannelKind.SMS => PhoneNumber ?? "",
        _ => RecipientId ?? ""
    };
}