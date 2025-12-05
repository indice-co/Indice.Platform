using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Indice.AspNetCore.Features.SignalrEnpoint;

public class HubContextStore
{
    /// <summary>
    /// The context hub dictionary, stores the hub contexts for each connection.
    /// </summary>
    private readonly ConcurrentDictionary<string, Lazy<Task<ServiceHubContext>>> _contexts = new();
    private readonly ServiceManager _serviceManager;
    
    public HubContextStore(ServiceManager serviceManager) {
        _serviceManager = serviceManager;
    }

    public Task<ServiceHubContext> GetHubContextAsync(string hubName, CancellationToken cancellationToken) {
        // The complex logic lives here, written ONCE.
        var lazy = _contexts.GetOrAdd(hubName, name =>
            new Lazy<Task<ServiceHubContext>>(() =>
            _serviceManager.CreateHubContextAsync(name, cancellationToken))
        );
        return lazy.Value;
    }
}
