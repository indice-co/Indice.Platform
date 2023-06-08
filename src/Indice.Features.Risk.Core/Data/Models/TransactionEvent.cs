#nullable disable
namespace Indice.Features.Risk.Core.Data.Models;

/// <summary></summary>
public class TransactionEvent
{
    /// <summary>The unique id of the event.</summary>
    public Guid Id { get; set; }
    /// <summary>The name of the event.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Timestamp regarding transaction creation.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Reference to parent transaction.</summary>
    public Guid TransactionId { get; set; }
    /// <summary>Reference to parent transaction.</summary>
    public virtual Transaction Transaction { get; set; }
}
#nullable disable
