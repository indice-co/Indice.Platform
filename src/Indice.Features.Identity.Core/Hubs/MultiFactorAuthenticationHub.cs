using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.Core.Hubs;

/// <summary>SignalR multi-factor authentication hub abstraction.</summary>
public interface IMultiFactorAuthenticationHub
{
    /// <summary>Called when a new connection is established with the hub.</summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous connect.</returns>
    Task OnConnectedAsync();
    /// <summary>Called when the user approves the sign in.</summary>
    /// <param name="connectionId">The connection id to the SignalR hub.</param>
    /// <param name="otpCode">User's OTP code, received via push notification.</param>
    Task LoginApproved(string connectionId, string otpCode);
    /// <summary>Called when the user rejects the sign in.</summary>
    /// <param name="connectionId">The connection id to the SignalR hub.</param>
    Task LoginRejected(string connectionId);
}

/// <inheritdoc />
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.TwoFactorUserIdScheme)]
public class MultiFactorAuthenticationHub : Hub, IMultiFactorAuthenticationHub
{
    private readonly ILogger<MultiFactorAuthenticationHub> _logger;

    /// <summary>Creates a new instance of the <see cref="MultiFactorAuthenticationHub"/> class.</summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MultiFactorAuthenticationHub(ILogger<MultiFactorAuthenticationHub> logger) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override Task OnConnectedAsync() {
        _logger.LogInformation("Connection established with the '{HubName}'", nameof(MultiFactorAuthenticationHub));
        return base.OnConnectedAsync();
    }

    /// <inheritdoc />
    [Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ApiAuthenticationScheme)]
    public async Task LoginApproved(string connectionId, string otpCode) {
        var userId = Context.UserIdentifier;
        if (userId is null) {
            throw new InvalidOperationException();
        }
        await Clients.Client(connectionId).SendAsync("LoginApproved", otpCode);
    }

    /// <inheritdoc />
    [Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ApiAuthenticationScheme)]
    public async Task LoginRejected(string connectionId) {
        var userId = Context.UserIdentifier;
        if (userId is null) {
            throw new InvalidOperationException();
        }
        await Clients.Client(connectionId).SendAsync("LoginRejected");
    }
}