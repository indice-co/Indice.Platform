using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Retrieves the customer/case data a conversation is grounded on from the system of record.
/// The default implementation calls an MCP tool; hosts with an in-process source can replace it.
/// </summary>
public interface ICustomerDataResolver
{
    /// <summary>
    /// Returns the payload behind <paramref name="reference"/>, or <c>null</c> when the external system
    /// knows nothing about it. Implementations must not leak the payload anywhere but the return value —
    /// it is undisclosed data until the caller has been verified against it.
    /// </summary>
    Task<CustomerDataRecord?> ResolveAsync(ExternalReference reference, CancellationToken cancellationToken = default);
}
