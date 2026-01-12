using System.Security.Claims;
using Indice.AspNetCore.Features.SignalREndpoint.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.SignalREndpoint;

/// <inheritdoc/>
public class SignalRNegotiateService : ISignalRNegotiateService
{
    private readonly IOptions<SignalREndpointsOptions> _options;
    private readonly HubContextStore _hubContextStore;

    /// <summary>
    /// Initializes a new instance of the SignalRNegotiateService.
    /// </summary>
    /// <param name="options">The SignalR endpoints configuration options.</param>
    /// <param name="hubContextStore">The hub context store for managing SignalR hub contexts.</param>
    public SignalRNegotiateService( IOptions<SignalREndpointsOptions> options, HubContextStore hubContextStore)
    {
        _options = options;
        _hubContextStore = hubContextStore;
    }

    /// <inheritdoc/>
    public async Task<NegotiateResponse> Negotiate(
    string hub,
    string userId,
    List<Claim> userClaims,
    CancellationToken cancellationToken) {

        var hubContext = await _hubContextStore.GetHubContextAsync(hub, cancellationToken);

        await AddUserToClaimGroups(hubContext, userClaims, userId, cancellationToken);
        var negotiationResponse = await hubContext.NegotiateAsync(new NegotiationOptions {
            TokenLifetime = TimeSpan.FromHours(1),
            UserId = userId
        }, cancellationToken);
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
    private async Task AddUserToClaimGroups(ServiceHubContext hubContext, List<Claim> userClaims, string userId, CancellationToken cancellationToken) {
        var userGroups = userClaims.Where(userClaim => _options.Value.GroupClaims.Contains(userClaim.Type, StringComparer.OrdinalIgnoreCase))
            .Select(c => c.Value).ToList();
        if (userGroups.Any()) {
            var groupAddTasks = userGroups.Select(groupName =>
                        hubContext.UserGroups.AddToGroupAsync(userId, groupName, TimeSpan.FromHours(1), cancellationToken));
            await Task.WhenAll(groupAddTasks);
        }
    }

    ///<inheritdoc/>
    public async Task AddUserToGroups(string hubName, List<string> userGroups, string userId, CancellationToken cancellationToken) {
        var hubContext = await _hubContextStore.GetHubContextAsync(hubName, cancellationToken);

        var groupAddTasks = userGroups.Select(groupName =>
                    hubContext.UserGroups.AddToGroupAsync(userId, groupName, TimeSpan.FromHours(1), cancellationToken));
        await Task.WhenAll(groupAddTasks);
    }


}
