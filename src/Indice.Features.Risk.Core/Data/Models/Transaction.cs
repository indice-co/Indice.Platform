namespace Indice.Features.Risk.Core.Data.Models;

/// <summary>Models an transaction that occurred in the system.</summary>
public class Transaction
{
    /// <summary>The unique id of the transaction.</summary>
    public Guid Id { get; internal set; }
    /// <summary>An amount relative to the transaction.</summary>
    public decimal? Amount { get; set; }
    /// <summary>The user IP address related to the transaction occurred.</summary>
    public string? IpAddress { get; set; }
    /// <summary>The unique identifier of the subject performing the transaction.</summary>
    public string? SubjectId { get; set; }
    /// <summary>Timestamp regarding transaction creation.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Events associated with the transaction.</summary>
    public virtual ICollection<TransactionEvent> Events { get; set; } = new List<TransactionEvent>();
}
