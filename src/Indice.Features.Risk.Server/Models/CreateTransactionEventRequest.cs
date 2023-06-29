using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Server.Models;

/// <summary></summary>
public class CreateTransactionEventRequest
{
    /// <summary>The name of the event.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Reference to parent transaction.</summary>
    public Guid TransactionId { get; set; }
}

/// <summary>Extension methods on <see cref="CreateTransactionEventRequest"/> type.</summary>
public static class CreateTransactionEventRequestExtensions
{
    /// <summary>Maps the current <see cref="CreateTransactionEventRequest"/> instance to <see cref="TransactionEvent"/> database model.</summary>
    public static TransactionEvent ToTransactionEvent(this CreateTransactionEventRequest command) => new() {
        Id = Guid.NewGuid(),
        TransactionId = command.TransactionId,
        CreatedAt = DateTimeOffset.UtcNow,
        Name = command.Name
    };
}
