namespace Indice.Features.Workers.Server;

/// <summary>The scopes of the workers API.</summary>
public static class WorkerScopes
{
    /// <summary>The scope of the workers dashboard. Used together with the <see cref="Indice.Security.BasicRoleNames.WorkersReader"/> and <see cref="Indice.Security.BasicRoleNames.WorkersAdmin"/> roles.</summary>
    public const string Workers = "workers";
    /// <summary>A sub-scope that lets a machine-to-machine client (client credentials) add messages to the queues.</summary>
    public const string Producer = "workers:producer";
    /// <summary>A sub-scope that lets a machine-to-machine client (client credentials) take messages from the queues. Reserved for the dequeue endpoints, which are not available yet.</summary>
    public const string Consumer = "workers:consumer";
}
