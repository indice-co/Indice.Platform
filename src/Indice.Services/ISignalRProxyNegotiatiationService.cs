using System.ComponentModel;

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
    Task<SignalRNegotiationResponse> NegotiateAsync(string hub, List<string> groupNamesToJoin, string? userId = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Add user to specified groups
    /// </summary>
    /// <param name="hubName"> The hub name </param>
    /// <param name="userId"> The user ID </param>
    /// <param name="userGroups"> The groups to add the user to </param>
    /// <param name="cancellationToken"> Cancellation Token </param>
    /// <returns></returns>
    Task AddUserToGroupsAsync(string hubName, string userId, List<string> userGroups, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add user to specified groups
    /// </summary>
    /// <param name="hubName"> The hub name </param>
    /// <param name="userId"> The user ID </param>
    /// <param name="userGroups"> The groups to remove the user from </param>
    /// <param name="cancellationToken"> Cancellation Token </param>
    /// <returns></returns>
    Task RemoveUserFromGroupsAsync(string hubName, string userId, List<string> userGroups, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a connection to specified groups.
    /// </summary>
    /// <param name="hubName"> The hub name </param>
    /// <param name="connectionId"> The connection ID </param>
    /// <param name="userGroups"> The groups to add the connection to </param>
    /// <param name="cancellationToken"> Cancellation Token </param>
    /// <returns></returns>
    Task AddConnectionToGroupsAsync(string hubName, string connectionId, List<string> userGroups, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a connection from specified groups.
    /// </summary>
    /// <param name="hubName"> The hub name </param>
    /// <param name="connectionId"> The connection ID </param>
    /// <param name="userGroups"> The groups to remove the connection from </param>
    /// <param name="cancellationToken"> Cancellation Token </param>
    /// <returns></returns>
    Task RemoveConnectionFromGroupsAsync(string hubName, string connectionId, List<string> userGroups, CancellationToken cancellationToken = default);


    }

    /// <summary>
    /// The Negotiation response.
    /// <param name="Url"></param>
    /// <param name="AccessToken"> The access token for the request. </param>
    /// </summary>
    public record SignalRNegotiationResponse([Description("The URL for the SignalR connection.")]string? Url, [Description("The access token for the request.")]string? AccessToken);


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
    public async Task<SignalRNegotiationResponse> NegotiateAsync(string hub, List<string> groupNamesToJoin, string? userId = null, CancellationToken cancellationToken = default) {
        var hubContext = await _hubContextStore.GetHubContextAsync(hub, cancellationToken);
        var negotiationResponse = await hubContext.NegotiateAsync(new () { TokenLifetime = DefaultTokenLifetime, UserId = userId }, cancellationToken);
        if (userId != null && groupNamesToJoin.Any()) { 
            await AddUserToGroupsAsync(hubName: hub, userId, userGroups: groupNamesToJoin, cancellationToken: cancellationToken);
        }
        return new (negotiationResponse.Url, negotiationResponse.AccessToken);
    }

    ///<inheritdoc/>
    public async Task AddUserToGroupsAsync(string hubName, string userId, List<string> userGroups, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
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

    ///<inheritdoc/>
    public async Task AddConnectionToGroupsAsync(string hubName, string connectionId, List<string> userGroups, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        var hubContext = await _hubContextStore.GetHubContextAsync(hubName, cancellationToken);
        var groupAddTasks = userGroups.Select(groupName => hubContext.Groups.AddToGroupAsync(connectionId, groupName, cancellationToken));
        await Task.WhenAll(groupAddTasks);
    }

    ///<inheritdoc/>

    public async Task RemoveConnectionFromGroupsAsync(string hubName, string connectionId, List<string> userGroups, CancellationToken cancellationToken = default) {
        var hubContext = await _hubContextStore.GetHubContextAsync(hubName, cancellationToken);
        var groupRemoveTasks = userGroups.Select(groupName => hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName, cancellationToken));
        await Task.WhenAll(groupRemoveTasks);
    }
}