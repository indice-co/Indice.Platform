using Indice.Types;

namespace Indice.Features.Messages.Core.Models.Requests;
/// <summary>Models a request to update a recipient's communication preferences.</summary>
public class UpdatPreferenceRequest
{
    /// <summary>Users's locale.</summary>
    public string? Locale { get; set; }
    /// <summary>Recepients communication preferences per type.</summary>
    public List<UpdateMessageTypePreference> CommunicationPreferences { get; set; } = [];
}
/// <summary>Models a contact preference for a recipient.</summary>
public class UpdateMessageTypePreference
{
    /// <summary>The id of the type or the Alias.</summary>
    public string? Alias { get; set; }
    /// <summary>The preferred delivery channels to receive messages.</summary>
    public List<ContactChannelKind> Channels { get; set; } = [ContactChannelKind.Any];
}