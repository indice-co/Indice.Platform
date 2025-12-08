using System.Security.Claims;
using Indice.SignalR.Endpoints;

namespace Indice.AspNetCore.Features.SignalrEnpoint.Interfaces;

/// <summary>
/// Interface for exposed SignalR methods.
/// </summary>
public interface ISignalRNegotiateService
{
    /// <summary>
    /// A way to negotiate a SignalR connection.
    /// </summary>
    /// <param name="hub"> The hub name </param>
    /// <param name="userId"> User Information </param>
    /// <param name="userClaims"> User Information </param>
    /// <param name="cancellationToken"> Cancellation Token </param>
    Task<NegotiateResponse> Negotiate(
        string hub,
        string userId,
        List<Claim> userClaims,
        CancellationToken cancellationToken);
}
