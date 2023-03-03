using Indice.AspNetCore.Identity.Api.Security;
using Indice.Features.Identity.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Indice.Identity.Hubs;

[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.TwoFactorUserIdScheme)]
public class MultiFactorAuthenticationHub : Hub
{
    private readonly ILogger<MultiFactorAuthenticationHub> _logger;

    public MultiFactorAuthenticationHub(ILogger<MultiFactorAuthenticationHub> logger) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task OnConnectedAsync() {
        _logger.LogInformation("Connection established with the '{HubName}'", nameof(MultiFactorAuthenticationHub));
        return base.OnConnectedAsync();
    }

    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme)]
    public async Task LoginApproved(string connectionId, string otpCode) {
        var userId = Context.UserIdentifier;
        if (userId is null) {
            throw new InvalidOperationException();
        }
        await Clients.Client(connectionId).SendAsync("LoginApproved", otpCode);
    }
}
