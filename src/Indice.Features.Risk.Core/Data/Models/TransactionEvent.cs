namespace Indice.Features.Risk.Core.Data.Models;

/// <summary>A database model to keep track of events happened for a transaction.</summary>
public class TransactionEvent
{
    /// <summary>The unique id of the event.</summary>
    public Guid Id { get; set; }
    /// <summary>The name of the event.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Timestamp regarding transaction creation.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Reference to parent transaction.</summary>
    public Guid TransactionId { get; set; }
    /// <summary>Reference to parent transaction.</summary>
    public virtual Transaction Transaction { get; set; } = null!;
}
