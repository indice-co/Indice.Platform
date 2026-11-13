using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Workers.Server;

/// <summary>Options for the workers dashboard HTTP API.</summary>
public class WorkerEndpointsOptions
{
    /// <summary>Route prefix of the API. Defaults to <c>/api/workers</c>.</summary>
    public PathString PathPrefix { get; set; } = "/api/workers";
    /// <summary>OpenAPI group name. Defaults to <c>workers</c>.</summary>
    public string GroupName { get; set; } = "workers";
    /// <summary>
    /// Scope that the client must have been granted. Defaults to <c>workers</c>. Set to <see langword="null"/> to skip the scope check.
    /// On top of the scope, the user needs a role: see <see cref="WorkerPolicies"/>.
    /// </summary>
    public string? RequiredScope { get; set; } = WorkerScopes.Workers;
    /// <summary>
    /// Sub-scope that lets a machine-to-machine client add messages to the queues (see <see cref="WorkerPolicies.BeWorkersProducer"/>).
    /// Defaults to <c>workers:producer</c>. Set to <see langword="null"/> to disable machine-to-machine publishing.
    /// </summary>
    public string? ProducerScope { get; set; } = WorkerScopes.Producer;
    /// <summary>
    /// Sub-scope that lets a machine-to-machine client take messages from the queues (see <see cref="WorkerPolicies.BeWorkersConsumer"/>).
    /// Defaults to <c>workers:consumer</c>. Set to <see langword="null"/> to disable it. Reserved for the dequeue endpoints, which are not available yet.
    /// </summary>
    public string? ConsumerScope { get; set; } = WorkerScopes.Consumer;
    /// <summary>
    /// How long the overall counters (<c>GET stats</c>) are cached, so that many open dashboards cost a single query per period. Defaults to 10 seconds.
    /// Set to <see cref="TimeSpan.Zero"/> to disable the cache.
    /// </summary>
    public TimeSpan StatsCacheDuration { get; set; } = TimeSpan.FromSeconds(10);
    /// <summary>The maximum length of a payload that can be enqueued or edited from the dashboard, in characters. Defaults to 1,000,000.</summary>
    public int MaxPayloadLength { get; set; } = 1_000_000;
    /// <summary>
    /// Configures the database that holds the worker tables (schema <c>work</c>).
    /// Defaults to SQL Server with the <c>WorkerDb</c> connection string, the same default as <c>UseStoreRelational()</c>.
    /// </summary>
    /// <remarks>Ignored when the host has already registered the dashboard <see cref="DbContext"/> type itself.</remarks>
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }
}
