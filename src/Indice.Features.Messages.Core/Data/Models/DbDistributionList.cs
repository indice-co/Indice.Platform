using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Data.Models
{
    /// <summary>Distribution list entity.</summary>
    public class DbDistributionList
    {
        /// <summary>The unique id.</summary>
        public Guid Id { get; set; }
        /// <summary>The name of the distribution list.</summary>
        public string Name { get; set; }
        /// <summary>When the distribution list was created.</summary>
        public DateTimeOffset CreatedAt { get; set; }
        /// <summary>Describes who created the record.</summary>
        public CreatedBy CreatedBy { get; set; }
        /// <summary>Contact - Distribution list join entity type.</summary>
        public List<DbDistributionListContact> ContactDistributionLists { get; set; } = new List<DbDistributionListContact>();
    }
}
