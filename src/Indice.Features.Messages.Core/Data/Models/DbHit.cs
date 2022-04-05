namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    /// <summary>
    /// Campaign hit entity.
    /// </summary>
    public class DbHit
    {
        /// <summary>
        /// The unique id of the record.
        /// </summary>
        public int Id { get; }
        /// <summary>
        /// Defines when hit occurred.
        /// </summary>
        public DateTimeOffset TimeStamp { get; set; }
        /// <summary>
        /// Foreign key to the campaign.
        /// </summary>
        public Guid CampaignId { get; set; }
    }
}
