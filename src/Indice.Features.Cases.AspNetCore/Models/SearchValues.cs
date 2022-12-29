namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// The search values model.
    /// </summary>
    public class SearchValues
    {
        /// <summary>
        /// An Id that can optionally used in lookup, e.g. a customerId
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The value of a Independent Field that can optionally used in lookup
        /// </summary>
        public string IndependentFieldValue { get; set; }

        /// <summary>
        /// The Category that can optionally used in lookup, e.g. product family
        /// </summary>
        public string Category { get; set; }
    }
}
