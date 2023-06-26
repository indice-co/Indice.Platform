using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Server.Models;

/// <summary>Models the request model used to create a transaction.</summary>
public class CreateTransactionRequestBase<TTransaction> where TTransaction : Transaction, new()
{
    /// <summary>The unique id of the transaction.</summary>
    public Guid? Id { get; set; }
    /// <summary>An amount relative to the transaction.</summary>
    public decimal? Amount { get; set; }
    /// <summary>The user IP address related to the transaction occurred.</summary>
    public string? IpAddress { get; set; }
    /// <summary>The unique identifier of the subject performing the transaction.</summary>
    public string? SubjectId { get; set; }

    /// <summary>Converts a <see cref="CreateTransactionRequestBase{TTransaction}"/> instance to a <see cref="Transaction"/> instance.</summary>
    public virtual TTransaction ToTransaction() => new() {
        Amount = Amount,
        CreatedAt = DateTimeOffset.UtcNow,
        Id = Id ?? Guid.NewGuid(),
        IpAddress = IpAddress,
        SubjectId = SubjectId
    };
}
