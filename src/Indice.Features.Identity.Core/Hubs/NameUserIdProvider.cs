using IdentityModel;
using Microsoft.AspNetCore.SignalR;

namespace Indice.Features.Identity.Core.Hubs;

/// <summary>Gets the user id from the <see cref="JwtClaimTypes.Name"/> claim.</summary>
public class NameUserIdProvider : IUserIdProvider
{
    /// <inheritdoc />
    public string? GetUserId(HubConnectionContext connection) => connection.User?.FindFirst(JwtClaimTypes.Name)?.Value;
}
