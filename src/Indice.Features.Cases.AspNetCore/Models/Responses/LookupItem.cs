namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// The lookup item model.
    /// </summary>
    public class LookupItem
    {
        /// <summary>
        /// The name or the key of the look up item
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The value of the lookup item
        /// </summary>
        public string Value { get; set; }
    }

    public class SearchValues
    {
        public string CustomerId { get; set; }
        public string DependentFieldValue { get; set; }
    }
}
