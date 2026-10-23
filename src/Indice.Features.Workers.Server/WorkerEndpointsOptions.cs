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
    /// <summary>Scope that grants access. Callers that are admins (<c>IsAdmin()</c>) are always allowed. Defaults to <c>workers</c>.</summary>
    public string? RequiredScope { get; set; } = "workers";
    /// <summary>
    /// Configures the database that holds the worker tables (schema <c>work</c>).
    /// Defaults to SQL Server with the <c>WorkerDb</c> connection string, the same default as <c>UseStoreRelational()</c>.
    /// </summary>
    /// <remarks>Ignored when the host has already registered the dashboard <see cref="DbContext"/> type itself.</remarks>
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }
}
