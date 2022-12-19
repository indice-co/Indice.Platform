using System.Threading.Tasks;
using Indice.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Hubs
{
    [Authorize(AuthenticationSchemes = ExtendedIdentityConstants.TwoFactorUserIdScheme)]
    public class MultiFactorAuthenticationHub : Hub
    {
        private readonly ILogger<MultiFactorAuthenticationHub> _logger;

        public MultiFactorAuthenticationHub(ILogger<MultiFactorAuthenticationHub> logger) {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public override Task OnConnectedAsync() {
            _logger.LogInformation("Connection established with the {HubName}", nameof(MultiFactorAuthenticationHub));
            return base.OnConnectedAsync();
        }
    }
}
