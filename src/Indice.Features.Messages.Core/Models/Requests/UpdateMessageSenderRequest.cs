namespace Indice.Features.Messages.Core.Models.Requests;
/// <summary>The request to update an existing sender object.</summary>
public class UpdateMessageSenderRequest
{
    /// <summary>The sender of the Email.</summary>
    public string Sender { get; set; }
    /// <summary>The display name of the sender.</summary>
    public string DisplayName { get; set; }
    /// <summary>Indicates that this is the default sender.</summary>
    public bool IsDefault { get; set; }
}
