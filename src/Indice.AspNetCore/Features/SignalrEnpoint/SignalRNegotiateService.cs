using System.Security.Claims;
using Indice.AspNetCore.Features.SignalrEnpoint.Interfaces;
using Indice.SignalR.Endpoints;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.SignalrEnpoint;

/// <inheritdoc></inheritdoc>/>
public class SignalRNegotiateService : ISignalRNegotiateService
{
    private readonly IOptions<SignalREndpointsOptions> _options;
    private readonly HubContextStore _hubContextStore;

    /// <summary>
    /// The signalR service
    /// </summary>
    /// <param name="serviceManager"></param>
    /// <param name="options"></param>
    public SignalRNegotiateService( IOptions<SignalREndpointsOptions> options, HubContextStore hubContextStore)
    {
        _options = options;
        _hubContextStore = hubContextStore;
    }

    public async Task<NegotiateResponse> Negotiate(
    string hub,
    string userId,
    List<Claim> userClaims,
    CancellationToken cancellationToken) {

        var hubContext = await _hubContextStore.GetHubContextAsync(hub, CancellationToken.None);

        await AddUserToGroups(hubContext, userClaims, userId, cancellationToken);
        var negotiationResponse = await hubContext.NegotiateAsync(new NegotiationOptions {
            TokenLifetime = TimeSpan.FromHours(1),
            UserId = userId
        });
        return new NegotiateResponse(
                negotiationResponse.Url,
                negotiationResponse.AccessToken);
    }

    /// <summary>
    /// Add a user to groups based on their claims.
    /// </summary>
    /// <param name="hubContext"></param>
    /// <param name="userId"></param>
    /// <param name="userClaims"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task AddUserToGroups(ServiceHubContext hubContext, List<Claim> userClaims, string userId, CancellationToken cancellationToken) {
        var userGroups = userClaims.Where(userClaim => _options.Value.GroupClaims.Contains(userClaim.Type, StringComparer.OrdinalIgnoreCase))
            .Select(c => c.Value).ToList();
        if (userGroups.Any()) {
            var groupAddTasks = userGroups.Select(groupName =>
                        hubContext.UserGroups.AddToGroupAsync(userId, groupName, cancellationToken));
            await Task.WhenAll(groupAddTasks);
        }
    }
}
