using Indice.Extensions.Configuration.Database.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Extensions.Configuration.Database;

/// <summary>Models the required interface needed to be inherited by a <see cref="DbContext"/> when an applications wants to use the database configuration feature.</summary>
public interface IAppSettingsDbContext
{
    /// <summary>The settings table.</summary>
    DbSet<DbAppSetting> AppSettings { get; set; }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken "/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous save operation. The task result contains
    /// the number of state entries written to the database.
    /// </returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}
