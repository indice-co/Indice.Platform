namespace Indice.Services;

/// <summary>
/// Interface for exposed SignalR methods.
/// </summary>
public interface ISignalRProxyNegotiatiationService
{
    /// <summary>
    /// A way to negotiate a SignalR connection.
    /// </summary>
    /// <param name="hub"> The hub name </param>
    /// <param name="userId"> User Information </param>
    /// <param name="groupNamesToJoin"> groupNames to join upon negotiation </param>
    /// <param name="cancellationToken"> Cancellation Token </param>
    Task<SignalRNegotiationResponse> NegotiateAsync(string hub, string userId, List<string> groupNamesToJoin, CancellationToken cancellationToken = default);
    /// <summary>
    /// Add user to specified groups
    /// </summary>
    /// <param name="hubName"></param>
    /// <param name="userId"></param>
    /// <param name="userGroups"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddUserToGroupsAsync(string hubName, string userId, List<string> userGroups, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add user to specified groups
    /// </summary>
    /// <param name="hubName"></param>
    /// <param name="userId"></param>
    /// <param name="userGroups"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task RemoveUserFromGroupsAsync(string hubName, string userId, List<string> userGroups, CancellationToken cancellationToken = default);

}

/// <summary>
/// The Negotiation response.
/// <param name="Url"></param>
/// <param name="AccessToken"> The access token for the request. </param>
/// </summary>
public record SignalRNegotiationResponse(string? Url, string? AccessToken);


/// <inheritdoc/>
public class SignalRProxyNegotiatiationService : ISignalRProxyNegotiatiationService
{
    private static readonly TimeSpan DefaultTokenLifetime = TimeSpan.FromHours(1);  
    private readonly SignalRProxyHubContextStore _hubContextStore;

    /// <summary>
    /// Initializes a new instance of the SignalRNegotiateService.
    /// </summary>
    /// <param name="hubContextStore">The hub context store for managing SignalR hub contexts.</param>
    public SignalRProxyNegotiatiationService(SignalRProxyHubContextStore hubContextStore) {
        _hubContextStore = hubContextStore;
    }

    /// <inheritdoc/>
    public async Task<SignalRNegotiationResponse> NegotiateAsync(string hub, string userId, List<string> groupNamesToJoin, CancellationToken cancellationToken = default) {
        var hubContext = await _hubContextStore.GetHubContextAsync(hub, cancellationToken);
        var negotiationResponse = await hubContext.NegotiateAsync(new () { TokenLifetime = DefaultTokenLifetime, UserId = userId }, cancellationToken);
        await AddUserToGroupsAsync(hubName: hub, userId, userGroups: groupNamesToJoin, cancellationToken: cancellationToken);

        return new (negotiationResponse.Url, negotiationResponse.AccessToken);
    }

    ///<inheritdoc/>
    public async Task AddUserToGroupsAsync(string hubName, string userId, List<string> userGroups, CancellationToken cancellationToken = default) {
        var hubContext = await _hubContextStore.GetHubContextAsync(hubName, cancellationToken);
        var groupAddTasks = userGroups.Select(groupName => hubContext.UserGroups.AddToGroupAsync(userId, groupName, DefaultTokenLifetime, cancellationToken));

        await Task.WhenAll(groupAddTasks);
    }

    ///<inheritdoc/>
    public async Task RemoveUserFromGroupsAsync(string hubName, string userId, List<string> userGroups, CancellationToken cancellationToken = default) {
        var hubContext = await _hubContextStore.GetHubContextAsync(hubName, cancellationToken);
        var groupRemoveTasks = userGroups.Select(groupName => hubContext.UserGroups.RemoveFromGroupAsync(userId, groupName, cancellationToken));

        await Task.WhenAll(groupRemoveTasks);

    }
}