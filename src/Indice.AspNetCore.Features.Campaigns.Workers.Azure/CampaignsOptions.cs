using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Features.Campaigns.Workers.Azure
{
    /// <summary>
    /// Options used when configuring campaign Azure Functions.
    /// </summary>
    public class CampaignsOptions
    {
        internal IServiceCollection Services { get; set; }
    }
}
