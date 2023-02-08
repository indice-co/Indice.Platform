using Indice.Features.Cases.Models;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// The admin/manage service regarding cases reports.
    /// </summary>
    public interface IAdminReportService
    {
        /// <summary>
        /// Get cases grouped by status
        /// </summary>
        /// <returns></returns>
        Task<List<GroupByReportResult>> GetCasesGroupedByStatus();

        /// <summary>
        /// Get cases grouped by casetype
        /// </summary>
        /// <returns></returns>
        Task<List<GroupByReportResult>> GetCasesGroupedByCaseType();

        /// <summary>
        /// Get cases grouped by GroupId
        /// </summary>
        /// <returns></returns>
        Task<List<GroupByReportResult>> GetCasesGroupedByGroupId();
    }
}
