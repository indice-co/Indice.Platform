using System;
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
        /// The case status filter.
        /// </summary>
        public List<CasePublicStatus>? PublicStatuses { get; set; }
        /// <summary>
        /// The case type code filter.
        /// </summary>
        public List<string>? CaseTypeCodes { get; set; }
        /// <summary>
        /// The CreatedFrom filter.
        /// </summary>
        public DateTime? CreatedFrom { get; set; }
        /// <summary>
        /// The CreatedTo filter.
        /// </summary>
        public DateTime? CreatedTo { get; set; }
        /// <summary>
        /// The CompletedFrom filter.
        /// </summary>
        public DateTime? CompletedFrom { get; set; }
        /// <summary>
        /// The CompletedTo filter.
        /// </summary>
        public DateTime? CompletedTo { get; set; }
    }
}