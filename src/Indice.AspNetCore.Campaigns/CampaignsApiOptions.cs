using System;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Features.Campaigns.Configuration
{
    /// <summary>
    /// Options used to configure the Campaigns API feature.
    /// </summary>
    public class CampaignsApiOptions
    {
        /// <summary>
        /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
        /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:DefaultConnection</i> to be present.
        /// </summary>
        public Action<DbContextOptionsBuilder> ConfigureDbContext { get; set; }
    }
}