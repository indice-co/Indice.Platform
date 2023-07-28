namespace Indice.Features.Messages.Core.Models.Requests;
/// <summary>The request model to create a new email sender.</summary>
public class CreateMessageSenderRequest
{
    /// <summary>The sender of the Email.</summary>
    public string Sender { get; set; }
    /// <summary>The display name of the sender of the email.</summary>
    public string DisplayName { get; set; }
}
