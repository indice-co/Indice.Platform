using Indice.Security;

namespace Indice.Features.Workers.Server;

/// <summary>Authorization policies of the workers API.</summary>
public static class WorkerPolicies
{
    /// <summary>Read only access: the user must have the <see cref="WorkerEndpointsOptions.RequiredScope"/> and be an admin (<c>IsAdmin()</c>) or own the <see cref="BasicRoleNames.WorkersReader"/> or <see cref="BasicRoleNames.WorkersAdmin"/> role.</summary>
    public const string BeWorkersReader = nameof(BeWorkersReader);
    /// <summary>Read and write access: the user must have the <see cref="WorkerEndpointsOptions.RequiredScope"/> and be an admin (<c>IsAdmin()</c>) or own the <see cref="BasicRoleNames.WorkersAdmin"/> role.</summary>
    public const string BeWorkersAdmin = nameof(BeWorkersAdmin);
    /// <summary>
    /// Adds messages to the queues: <see cref="BeWorkersAdmin"/>, or the <see cref="WorkerEndpointsOptions.ProducerScope"/> sub-scope.
    /// With the sub-scope, a machine-to-machine client (no subject) is enough. A user also needs one of the roles of <see cref="BeWorkersAdmin"/>.
    /// </summary>
    public const string BeWorkersProducer = nameof(BeWorkersProducer);
    /// <summary>
    /// Takes messages from the queues: a machine-to-machine client (no subject) with the <see cref="WorkerEndpointsOptions.ConsumerScope"/> sub-scope.
    /// Reserved for the dequeue endpoints, which are not available yet.
    /// </summary>
    public const string BeWorkersConsumer = nameof(BeWorkersConsumer);
}
