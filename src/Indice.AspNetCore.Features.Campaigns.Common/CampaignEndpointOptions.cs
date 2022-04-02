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
        /// <summary>
        /// Creates a new instance of <see cref="CampaignEndpointOptions"/>.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public CampaignEndpointOptions(IServiceCollection services) : base(services) { }

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
        /// <summary>
        /// Creates a new instance of <see cref="CampaignManagementOptions"/>.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public CampaignManagementOptions(IServiceCollection services) : base(services) { }

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
        /// <summary>
        /// Creates a new instance of <see cref="CampaignInboxOptions"/>.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public CampaignInboxOptions(IServiceCollection services) : base(services) { }

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
        private string _apiPrefix;

        /// <summary>
        /// Creates a new instance of <see cref="CampaignOptionsBase"/>.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public CampaignOptionsBase(IServiceCollection services) {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Creates a new instance of <see cref="CampaignOptionsBase"/>.
        /// </summary>
        public CampaignOptionsBase() { }

        /// <summary>
        /// Specifies the contract for a collection of service descriptors.
        /// </summary>
        public IServiceCollection Services { get; }
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
        /// Specifies a prefix for the API endpoints.
        /// </summary>
        public string ApiPrefix {
            get => _apiPrefix;
            set { _apiPrefix = string.IsNullOrWhiteSpace(value) ? "/" : value; }
        }
        /// <summary>
        /// The default scope name to be used for Campaigns API. Defaults to <see cref="CampaignsApi.Scope"/>.
        /// </summary>
        public string RequiredScope { get; set; } = CampaignsApi.Scope;
    }
}
