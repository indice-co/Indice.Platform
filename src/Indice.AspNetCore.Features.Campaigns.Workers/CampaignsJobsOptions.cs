using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    /// <summary>
    /// Options for configuring internal campaign jobs used by the worker host.
    /// </summary>
    public class CampaignsJobsOptions 
    {
        internal IServiceCollection Services { get; set; }
        /// <summary>
        /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
        /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:CampaignsDbConnection</i> to be present.
        /// </summary>
        public Action<DbContextOptionsBuilder> ConfigureDbContext { get; set; }
        /// <summary>
        /// Schema name used for tables. Defaults to <i>campaign</i>.
        /// </summary>
        public string DatabaseSchema { get; set; } = CampaignsApi.DatabaseSchema;
    }
}
