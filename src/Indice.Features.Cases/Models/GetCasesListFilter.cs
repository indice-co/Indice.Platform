using System;
using System.Collections.Generic;
using Indice.Types;

namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// Options used to filter the list of cases.
    /// 
    /// Will be used internally to filter cases further, based on authentication parameters
    /// </summary>
    public class GetCasesListFilter
    {
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public List<string>? CaseTypeCodes { get; set; }
        public List<string>? CheckpointTypeCodes { get; set; }
        public List<string>? GroupIds { get; set; }

        /// <summary>
        /// construct filter clauses based on the metadata you are adding to the cases in your installation
        /// </summary>
        public List<FilterClause>? Metadata { get; set; }
    }
}
