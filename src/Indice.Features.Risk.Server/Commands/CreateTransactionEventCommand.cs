using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Server.Commands;

/// <summary></summary>
public class CreateTransactionEventCommand
{
    /// <summary>The name of the event.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Reference to parent transaction.</summary>
    public Guid TransactionId { get; set; }
}

/// <summary>Extension methods on <see cref="CreateTransactionEventCommand"/> type.</summary>
public static class CreateTransactionEventCommandExtensions 
{
    /// <summary>Maps the current <see cref="CreateTransactionEventCommand"/> instance to <see cref="TransactionEvent"/> database model.</summary>
    public static TransactionEvent ToTransactionEvent(this CreateTransactionEventCommand command) => new() {
        Id = Guid.NewGuid(),
        TransactionId = command.TransactionId,
        CreatedAt = DateTimeOffset.UtcNow,
        Name = command.Name
    };
}
