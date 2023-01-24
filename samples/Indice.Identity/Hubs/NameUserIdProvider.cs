using IdentityModel;
using Microsoft.AspNetCore.SignalR;

namespace Indice.Identity.Hubs
{
    public class NameUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection) => connection.User?.FindFirst(JwtClaimTypes.Name)?.Value;
    }
}
