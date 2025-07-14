using Indice.Types;

namespace Indice.Features.Messages.Core.Models.Requests;
/// <summary>Models a request to update a recipient's communication preferences.</summary>
public class UpdateCommunicationPreferenceRequest
{
    /// <summary>Users's locale.</summary>
    public string? Locale { get; set; }
    /// <summary>Recepients communication preferences per type.</summary>
    public List<UpdateCommunicationMessageTypePreferenceRequest> CommunicationPreferencesPerMessageType { get; set; } = [];
}
/// <summary>Models a contact preference for a recipient.</summary>
public class UpdateCommunicationMessageTypePreferenceRequest
{
    /// <summary>The id of the type or the Alias.</summary>
    public GuidOrAlias? TypeId { get; set; }
    /// <summary>The preferred delivery channels to receive messages.</summary>
    public ContactChannelKind CommunicationPreferences { get; set; } = ContactChannelKind.Any;
}
