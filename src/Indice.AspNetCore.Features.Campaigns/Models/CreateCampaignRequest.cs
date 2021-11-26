using System.Collections.Generic;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// The request model used to create a new campaign.
    /// </summary>
    public class CreateCampaignRequest : UpdateCampaignRequest
    {
        /// <summary>
        /// Determines if campaign targets all user base.
        /// </summary>
        public bool IsGlobal { get; set; }
        /// <summary>
        /// Defines a list of user identifiers that constitutes the audience of the campaign.
        /// </summary>
        public List<string> SelectedUserCodes { get; set; } = new List<string>();
        /// <summary>
        /// Defines a CTA (click-to-action) URL.
        /// </summary>
        public string ActionUrl { get; set; }
    }
}
