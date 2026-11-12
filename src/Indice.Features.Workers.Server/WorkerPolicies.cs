using Indice.Security;

namespace Indice.Features.Workers.Server;

/// <summary>Authorization policies of the workers dashboard API. Both require the <see cref="WorkerEndpointsOptions.RequiredScope"/>.</summary>
public static class WorkerPolicies
{
    /// <summary>Read only access: the user must be an admin (<c>IsAdmin()</c>) or own the <see cref="BasicRoleNames.WorkersReader"/> or <see cref="BasicRoleNames.WorkersAdmin"/> role.</summary>
    public const string BeWorkersReader = nameof(BeWorkersReader);
    /// <summary>Read and write access: the user must be an admin (<c>IsAdmin()</c>) or own the <see cref="BasicRoleNames.WorkersAdmin"/> role.</summary>
    public const string BeWorkersAdmin = nameof(BeWorkersAdmin);
}
