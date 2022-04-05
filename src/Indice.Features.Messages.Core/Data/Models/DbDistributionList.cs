namespace Indice.Features.Messages.Core.Data.Models
{
    /// <summary>
    /// Distribution list entity.
    /// </summary>
    public class DbDistributionList
    {
        /// <summary>
        /// The unique id.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The name of the distribution list.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Navigation property to contacts.
        /// </summary>
        public virtual ICollection<DbContact> Contacts { get; set; }
    }
}
