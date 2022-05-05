namespace Indice.Features.Messages.Core.Models.Requests
{
    /// <summary>Options used to filter the list of contacts.</summary>
    public class ContactListFilter
    {
        /// <summary>The id of a distribution list.</summary>
        public Guid? DistributionListId { get; set; }
    }
}
