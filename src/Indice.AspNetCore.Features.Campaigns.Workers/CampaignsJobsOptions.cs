using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    /// <summary>
    /// Options for configuring internal campaign jobs used by the worker host.
    /// </summary>
    public class CampaignsJobsOptions 
    {
        internal IServiceCollection Services { get; set; }
    }
}
