using Indice.Types;

namespace Indice.Features.Messages.Core.Models.Requests;
/// <summary>Models a request to update a recipient's communication preferences.</summary>
public class UpdatePreferenceRequest
{
    /// <summary>Users's locale.</summary>
    public string? Locale { get; set; }
    /// <summary>Indicates if user accepted conset to receive email.</summary>
    public bool ConsentCommercial { get; set; }
    /// <summary>Indicates if user accepted conset to receive email.</summary>
    public DateTimeOffset? ConsentCommercialDate { get; set; }
    /// <summary>Recepients communication preferences per type.</summary>
    public List<UpdateMessageTypePreference> Communication { get; set; } = [];

    /// <summary>Default communication preferences</summary>
    public List<ContactChannelOption>? DefaultChannels { get; set; }
}

/// <summary>Models a contact preference for a recipient.</summary>
public class UpdateMessageTypePreference
{
    /// <summary>The alias of a campaign type.</summary>
    public GuidOrAlias MessageTypeAlias { get; set; }
    /// <summary>The preferred delivery channels to receive messages.</summary>
    public List<ContactChannelOption> Channels { get; set; } = ContactChannelOption.FromKindFlags(ContactChannelKind.Any);
}