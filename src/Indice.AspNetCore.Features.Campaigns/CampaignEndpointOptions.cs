using System;
using Indice.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Features.Campaigns
{
    /// <summary>
    /// Options used to configure the Campaigns API feature.
    /// </summary>
    public class CampaignEndpointOptions : CampaignOptionsBase
    {
        internal CampaignEndpointOptions(IServiceCollection services) : base(services) { }

        /// <summary>
        /// Creates a new instance of <see cref="CampaignEndpointOptions"/>.
        /// </summary>
        public CampaignEndpointOptions() : base() { }
    }

    /// <summary>
    /// Options used to configure the Campaigns management API feature.
    /// </summary>
    public class CampaignManagementOptions : CampaignOptionsBase
    {
        internal CampaignManagementOptions(IServiceCollection services) : base(services) { }

        /// <summary>
        /// Creates a new instance of <see cref="CampaignManagementOptions"/>.
        /// </summary>
        public CampaignManagementOptions() : base() { }
    }

    /// <summary>
    /// Options used to configure the Campaigns inbox API feature.
    /// </summary>
    public class CampaignInboxOptions : CampaignOptionsBase
    {
        internal CampaignInboxOptions(IServiceCollection services) : base(services) { }

        /// <summary>
        /// Creates a new instance of <see cref="CampaignInboxOptions"/>.
        /// </summary>
        public CampaignInboxOptions() : base() { }
    }

    /// <summary>
    /// Base class for Campaigns API options.
    /// </summary>
    public class CampaignOptionsBase
    {
        internal CampaignOptionsBase(IServiceCollection services) {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Creates a new instance of <see cref="CampaignOptionsBase"/>.
        /// </summary>
        public CampaignOptionsBase() { }

        internal IServiceCollection Services { get; set; }
        /// <summary>
        /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
        /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:CampaignsDbConnection</i> to be present.
        /// </summary>
        public Action<DbContextOptionsBuilder> ConfigureDbContext { get; set; }
        /// <summary>
        /// The claim type used to identify the user. Defaults to <i>sub</i>.
        /// </summary>
        public string UserClaimType { get; set; } = BasicClaimTypes.Subject;
        /// <summary>
        /// Schema name used for tables. Defaults to <i>campaign</i>.
        /// </summary>
        public string DatabaseSchema { get; set; } = CampaignsApi.DatabaseSchema;
        /// <summary>
        /// Specifies a prefix for the API endpoints. Defaults to <i>api</i>.
        /// </summary>
        public string ApiPrefix { get; set; }
        /// <summary>
        /// The default scope name to be used for Campaigns API. Defaults to <see cref="CampaignsApi.Scope"/>.
        /// </summary>
        public string RequiredScope { get; set; } = CampaignsApi.Scope;
    }
}
