namespace Indice.Features.Messages.Core.Models.Requests;
/// <summary>Options used to filter the Message Senders list.</summary>
public class MessageSenderListFilter
{
    /// <summary>Indicates that only the default sender should be returned.</summary>
    public bool? IsDefault { get; set; }
}
