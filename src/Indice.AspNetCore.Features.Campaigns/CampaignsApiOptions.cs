using System;
using Indice.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Features.Campaigns
{
    /// <summary>
    /// Options used to configure the Campaigns API feature.
    /// </summary>
    public class CampaignsApiOptions
    {
        internal IServiceCollection Services { get; set; }
        /// <summary>
        /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
        /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:DefaultConnection</i> to be present.
        /// </summary>
        public Action<DbContextOptionsBuilder> ConfigureDbContext { get; set; }
        /// <summary>
        /// The default scope name to be used for Campaigns API. Defaults to <see cref="CampaignsApi.Scope"/>.
        /// </summary>
        public string RequiredScope { get; set; } = CampaignsApi.Scope;
        /// <summary>
        /// Specifies a prefix for the API endpoints. Defaults to <i>api</i>.
        /// </summary>
        public string ApiPrefix { get; set; } = "api";
        /// <summary>
        /// The claim type used to identify the user. Defaults to <i>sub</i>.
        /// </summary>
        public string UserClaimType { get; set; } = BasicClaimTypes.Subject;
        /// <summary>
        /// Schema name used for tables. Defaults to <i>campaign</i>.
        /// </summary>
        public string DatabaseSchema { get; set; } = CampaignsApi.DatabaseSchema;
    }
}
