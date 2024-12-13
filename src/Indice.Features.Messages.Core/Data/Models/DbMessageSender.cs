using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Message sender.</summary>
public class DbMessageSender : DbAuditableEntity
{
    /// <summary>Id.</summary>
    public Guid Id { get; set; }
    /// <summary>Sender id.</summary>
    public string Sender { get; set; } = null!;
    /// <summary>Sender Name.</summary>
    public string? DisplayName { get; set; }
    /// <summary>Channel kind.</summary>
    public MessageChannelKind Kind { get; set; }
    /// <summary>Indicates that the Sender is the default.</summary>
    public bool IsDefault { get; set; }
}
