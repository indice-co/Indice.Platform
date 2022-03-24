namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// Models a request when creating a distribution list.
    /// </summary>
    public class CreateDistributionListRequest
    {
        /// <summary>
        /// The name of the distribution list.
        /// </summary>
        public string Name { get; set; }
    }
}
