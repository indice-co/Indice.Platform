namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Commnucation preferences of recipient entity.</summary>
public class DbContactPreference
{
    /// <summary>The unique id of the contact preference.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>The recipient correlation code.</summary>
    public string RecipientId { get; set; } = null!;
    /// <summary>Contact's locale.</summary>
    public string? Locale { get; set; }
    /// <summary>Indicates if user accepted conset to receive email.</summary>
    public bool ConsentCommercial { get; set; } = false;
    /// <summary>Indicates if user accepted conset to receive email.</summary>
    public DateTimeOffset? ConsentCommercialDate { get; set; }
    /// <summary>Communication preferences per message type.</summary>
    public List<DbContactCommunicationOption> CommunicationOptions { get; set; } = [];
    /// <summary>Indicates when record was last updated.</summary>
    public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
