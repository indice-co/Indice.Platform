using System;
using System.Text.Json.Serialization;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// The request model used to create a new campaign type.
    /// </summary>
    public class UpsertCampaignTypeRequest
    {
        /// <summary>
        /// The id of the campaign type.
        /// </summary>
        [JsonIgnore]
        public Guid Id { get; internal set; }
        /// <summary>
        /// The name of a campaign type.
        /// </summary>
        public string Name { get; set; }
    }
}
