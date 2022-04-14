namespace Indice.Features.Messages.Core.Data.Models
{
    /// <summary>
    /// Distribution list and contact entities relationship.
    /// </summary>
    public class DbContactDistributionList
    {
        /// <summary>
        /// The id of the distrubution list.
        /// </summary>
        public Guid DistributionListId { get; set; }
        /// <summary>
        /// The distrubution list navigation property.
        /// </summary>
        public DbDistributionList DistributionList { get; set; }
        /// <summary>
        /// The id of the contact.
        /// </summary>
        public Guid ContactId { get; set; }
        /// <summary>
        /// The contact navigation property
        /// </summary>
        public DbContact Contact { get; set; }
    }
}
