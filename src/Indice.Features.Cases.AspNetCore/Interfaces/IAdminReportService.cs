using System.Security.Claims;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// The admin/manage service regarding cases reports.
    /// </summary>
    public interface IAdminReportService
    {
        /// <summary>
        /// Generates a case report
        /// </summary>
        /// <param name="user"></param>
        /// <param name="reportTag"></param>
        /// <returns></returns>
        Task<List<GroupByReportResult>> GenerateReport(ClaimsPrincipal user, ReportTag reportTag);
    }
}
