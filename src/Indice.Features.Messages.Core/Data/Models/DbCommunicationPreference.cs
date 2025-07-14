namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Commnucation preferences of recipient entity.</summary>
public class DbCommunicationPreference
{
    /// <summary>The unique id of the contact preference.</summary>
    public required Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>The recipient correlation code.</summary>
    public required string RecipientId { get; set; }
    /// <summary>Contact's locale.</summary>
    public string? Locale { get; set; }
    /// <summary>Communication preferences per message type.</summary>
    public List<DbCommunicationPreferenceMessageType> MessageTypeCommunicationPreferences { get; set; } = [];
}
