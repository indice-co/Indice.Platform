using System.Collections.Generic;
using Indice.Features.Cases.Data.Models;

namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// Options used to filter the list of MyCases.
    /// </summary>
    public class GetMyCasesListFilter
    {
        /// <summary>
        /// The case type tag filter.
        /// </summary>
        public IEnumerable<string>? CaseTypeTags { get; set; }
        /// <summary>
        /// The current status of the case.
        /// </summary>
        public CasePublicStatus? PublicStatus { get; set; }
        /// <summary>
        /// The checkpoint name of the case.
        /// </summary>
        public List<string>? CheckpointNames { get; set; }
    }
}