namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// The request model used to create a new campaign type.
    /// </summary>
    public class CreateCampaignTypeRequest
    {
        /// <summary>
        /// The name of a campaign type.
        /// </summary>
        public string Name { get; set; }
    }
}
