using System.Collections.Concurrent;
using Indice.SignalR.Endpoints;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.SignalrEnpoint;

/// <summary>
/// Provides access to the SignalR hub contexts. Enhances reuseability.
/// </summary>
public class HubContextStore
{

    /// <summary>
    /// The context hub dictionary, stores the hub contexts for each connection.
    /// </summary>
    private readonly ConcurrentDictionary<string, Lazy<Task<ServiceHubContext>>> _contexts = new();
    private readonly ServiceManager _serviceManager;

    /// <summary>
    /// HubContext constructor.
    /// </summary>
    /// <param name="serviceManager"></param>
    public HubContextStore(ServiceManager serviceManager) {
        _serviceManager = serviceManager;
    }

    /// <summary>
    /// Return a Task that represents the asynchronous operation to get the hub context.
    /// </summary>
    /// <param name="hubName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ServiceHubContext> GetHubContextAsync(string hubName, CancellationToken cancellationToken) {

        var lazy = _contexts.GetOrAdd(hubName, name =>
            new Lazy<Task<ServiceHubContext>>(() =>
            _serviceManager.CreateHubContextAsync(name, cancellationToken))
        );
        return lazy.Value;
    }
}
