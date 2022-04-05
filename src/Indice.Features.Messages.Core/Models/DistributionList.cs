namespace Indice.Features.Messages.Core.Models
{
    /// <summary>
    /// Models a distribution list.
    /// </summary>
    public class DistributionList
    {
        /// <summary>
        /// The unique id.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The name of the distribution list.
        /// </summary>
        public string Name { get; set; }
    }
}
