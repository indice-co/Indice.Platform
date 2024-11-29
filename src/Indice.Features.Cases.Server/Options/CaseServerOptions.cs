using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Server.Options;

/// <summary>Configuration Options for the Case management server.</summary>
public class CaseServerOptions
{

    /// <summary>
    /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
    /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:DefaultConnection</i> to be present.
    /// </summary>
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }
}
