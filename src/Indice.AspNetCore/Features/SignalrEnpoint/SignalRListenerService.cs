using System.Security.Claims;
using Indice.AspNetCore.Features.SignalrEnpoint.Interfaces;
using Indice.Security;
using Indice.SignalR.Endpoints;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.SignalrEnpoint;

public class SignalRListenerService : ISignalRListenerService
{
    private readonly IOptions<SignalREndpointsOptions> _options;
    private readonly HubContextStore _hubContextStore;

    /// <summary>
    /// The signalR service
    /// </summary>
    /// <param name="serviceManager"></param>
    /// <param name="options"></param>
    public SignalRListenerService( IOptions<SignalREndpointsOptions> options, HubContextStore hubContextStore)
    {
        _options = options;
        _hubContextStore = hubContextStore;
    }

    public async Task<NegotiateResponse> Negotiate(
    string hub,
    ClaimsPrincipal currentUser,
    CancellationToken cancellationToken) {

        var hubContext = await _hubContextStore.GetHubContextAsync(hub, CancellationToken.None);
        var userId = currentUser.FindSubjectId();
        //only for testing purposes
        var allUserGroups = currentUser.Claims.ToList();

        // Add user to groups based on claims
        await AddUserToGroups(hubContext, currentUser, userId, cancellationToken);

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
    /// <param name="currentUser"></param>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task AddUserToGroups(ServiceHubContext hubContext, ClaimsPrincipal currentUser, string userId, CancellationToken cancellationToken) {
        var userGroups = currentUser.Claims.Where(userClaim => _options.Value.GroupClaims.Contains(userClaim.Type, StringComparer.OrdinalIgnoreCase))
            .Select(c => c.Value).ToList();
        if (userGroups.Any()) {
            var groupAddTasks = userGroups.Select(groupName =>
                        hubContext.UserGroups.AddToGroupAsync(userId, groupName, cancellationToken));
            await Task.WhenAll(groupAddTasks);
        }
    }


}
