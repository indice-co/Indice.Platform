using System.Collections.Generic;

namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// Options used to filter the list of MyCaseTypes.
    /// </summary>
    public class GetMyCaseTypesListFilter
    {
        /// <summary>
        /// The case type tag filter.
        /// </summary>
        public IEnumerable<string>? CaseTypeTags { get; set; }
    }
}