using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Indice.Features.Cases.Interfaces
{
    public interface ICaseWorkflowService
    {
        /// <summary>
        /// Get the <see cref="topResults"/> for a <see cref="checkpointTypeNameSuffix"/> (eg "Submitted").
        /// 
        /// todo Create generic Get with ListOptions && Filters && Ordering
        /// </summary>
        /// <param name="topResults">Top results to return.</param>
        /// <param name="checkpointTypeNameSuffix">The checkpoint type suffix to fetch (eg "Submitted").</param>
        /// <param name="caseTypeCode">The CaseType Code to filter the results. Optional.</param>
        /// <returns></returns>
        Task<IEnumerable<Guid>> GetCaseIdsForUpdate(int topResults, string checkpointTypeNameSuffix, string? caseTypeCode = null);
    }
}