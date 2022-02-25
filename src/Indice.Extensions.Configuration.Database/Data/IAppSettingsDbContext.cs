using Indice.Extensions.Configuration.Database.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Extensions.Configuration.Database
{
    /// <summary>
    /// Models the required interface needed to be inherited by a <see cref="DbContext"/> when an applications wants to use the database configuration feature.
    /// </summary>
    public interface IAppSettingsDbContext
    {
        /// <summary>
        /// The settings table.
        /// </summary>
        DbSet<AppSetting> AppSettings { get; set; }
    }
}
