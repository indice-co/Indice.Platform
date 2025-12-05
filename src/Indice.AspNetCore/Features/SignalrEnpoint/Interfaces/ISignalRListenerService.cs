using System.Security.Claims;
using Indice.SignalR.Endpoints;

namespace Indice.AspNetCore.Features.SignalrEnpoint.Interfaces;

/// <summary>
/// Interface for exposed SignalR methods.
/// </summary>
public interface ISignalRListenerService
{
    /// <summary>
    /// A way to negotiate a SignalR connection.
    /// </summary>
    /// <param name="hub"> The hub name </param>
    /// <param name="currentUser"> User Information </param>
    /// <param name="cancellationToken"> Cancellation Token </param>
    Task<NegotiateResponse> Negotiate(
        string hub,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken);
}
