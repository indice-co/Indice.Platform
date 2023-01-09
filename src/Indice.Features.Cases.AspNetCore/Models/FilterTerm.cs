namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// The Filter Term model.
    /// </summary>
    public class FilterTerm
    {
        /// <summary>
        /// FilterTerm's Key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// FilterTerm's Value
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// The Lookup Filter model.
    /// </summary>
    public class LookupFilter
    {
        /// <summary>
        /// A list of FilterTerms
        /// </summary>
        public List<FilterTerm> FilterTerms { get; set; }
    }
}
