using System;
using System.Collections.Generic;
using Indice.Features.Cases.Data.Models;
using Indice.Types;

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
        public DateTimeOffset? CreatedFrom { get; set; }
        /// <summary>
        /// The CreatedTo filter.
        /// </summary>
        public DateTimeOffset? CreatedTo { get; set; }
        /// <summary>
        /// The CompletedFrom filter.
        /// </summary>
        public DateTimeOffset? CompletedFrom { get; set; }
        /// <summary>
        /// The CompletedTo filter.
        /// </summary>
        public DateTimeOffset? CompletedTo { get; set; }
        /// <summary>
        /// Construct filter clauses based on case data.
        /// </summary>
        public List<FilterClause>? Data { get; set; }
        /// <summary>
        /// Construct filter clauses based on case metadata.
        /// </summary>
        public List<FilterClause>? Metadata { get; set; }
    }
}